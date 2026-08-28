using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.Infrastructure.Administration;

/// <summary>Vínculo N:N Centro de Custo × Unidade de Alocação (O1.9, ADR-0020 item 6, D4/ADR-0021).
///
/// Referencia o Centro de Custo pela identidade canônica local já estabelecida na O1.7
/// (<see cref="BlueprintOS.Domain.Identity.CentroCustoMetadado"/>) — nunca cria uma segunda fonte canônica
/// local. Sem escrita no ERP. Sem relação com a autorização de acesso Usuário×Centro de Custo (O1.6) —
/// vínculos distintos, propositalmente independentes (ADR-0020, itens 6/9).</summary>
public sealed class ListarVinculosUnidadeAlocacaoUseCase(
    ICentroCustoErpReader reader,
    ICentroCustoMetadadoRepository metadados,
    ICentroCustoUnidadeAlocacaoRepository vinculos,
    IUnidadeAlocacaoRepository unidadesAlocacao) : IListarVinculosUnidadeAlocacaoUseCase
{
    public async Task<ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>> ExecuteAsync(
        string codigoErp, Guid unidadeNegocioId, CancellationToken ct)
    {
        var codigo = (codigoErp ?? string.Empty).Trim();
        var erp = await reader.BuscarPorCodigoAsync(codigo, ct);
        if (erp is null)
        {
            return ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>.Erro(
                ErpMetadadoFalha.CodigoErpNaoEncontrado, "Código ERP de Centro de Custo não encontrado.");
        }

        // Leitura pura: se ainda não existe metadado local para este código nesta Unidade de Negócio,
        // não há vínculo possível ainda — nunca cria o metadado apenas para responder a uma leitura.
        var local = await metadados.ObterPorCodigoErpAsync(codigo, unidadeNegocioId, ct);
        if (local is null)
        {
            return ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>.Ok([]);
        }

        var dtos = await ProjetarAsync(vinculos, unidadesAlocacao, local.Id, unidadeNegocioId, ct);
        return ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>.Ok(dtos);
    }

    internal static async Task<IReadOnlyList<UnidadeAlocacaoVinculoDto>> ProjetarAsync(
        ICentroCustoUnidadeAlocacaoRepository vinculos, IUnidadeAlocacaoRepository unidadesAlocacao,
        Guid centroCustoMetadadoId, Guid unidadeNegocioId, CancellationToken ct)
    {
        var vinculados = await vinculos.ListarPorCentroCustoMetadadoAsync(centroCustoMetadadoId, ct);
        if (vinculados.Count == 0) return [];

        var unidades = await unidadesAlocacao.ObterPorIdsEUnidadeNegocioAsync(
            vinculados.Select(v => v.UnidadeAlocacaoId).ToArray(), unidadeNegocioId, ct);
        var porId = unidades.ToDictionary(u => u.Id);

        return vinculados
            .Where(v => porId.ContainsKey(v.UnidadeAlocacaoId))
            .Select(v => new UnidadeAlocacaoVinculoDto(v.UnidadeAlocacaoId, porId[v.UnidadeAlocacaoId].Nome, porId[v.UnidadeAlocacaoId].EstaAtiva(), v.Padrao))
            .OrderBy(v => v.Nome, StringComparer.Ordinal)
            .ToArray();
    }
}

/// <summary>Substitui integralmente o conjunto de Unidades de Alocação vinculadas a um Centro de Custo,
/// e define, entre elas, qual é a padrão (ADR-0020, item 6). Idempotente — mesmo padrão de
/// <c>SubstituirPerfisAsync</c>/<c>SubstituirCentrosCustoAsync</c> (O1.6).</summary>
public sealed class SubstituirVinculosUnidadeAlocacaoUseCase(
    ICentroCustoErpReader reader,
    ICentroCustoMetadadoRepository metadados,
    ICentroCustoUnidadeAlocacaoRepository vinculos,
    IUnidadeAlocacaoRepository unidadesAlocacao,
    TimeProvider clock) : ISubstituirVinculosUnidadeAlocacaoUseCase
{
    public async Task<ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>> ExecuteAsync(
        string codigoErp, SubstituirVinculosUnidadeAlocacaoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var codigo = (codigoErp ?? string.Empty).Trim();
        var erp = await reader.BuscarPorCodigoAsync(codigo, ct);
        if (erp is null)
        {
            return ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>.Erro(
                ErpMetadadoFalha.CodigoErpNaoEncontrado, "Código ERP de Centro de Custo não encontrado.");
        }

        var ids = (input.UnidadeAlocacaoIds ?? []).Distinct().ToArray();

        // Isolamento cross-BU: todo Id informado deve existir E pertencer à Unidade de Negócio da sessão —
        // um Id válido de outra Unidade de Negócio nunca é aceito (mesmo cuidado de PerfisRequisitados/O1.5
        // e da resolução de Centro de Custo do vínculo Usuário×Centro de Custo, O1.6-L2).
        if (ids.Length > 0)
        {
            var encontrados = await unidadesAlocacao.ObterPorIdsEUnidadeNegocioAsync(ids, unidadeNegocioId, ct);
            if (encontrados.Count != ids.Length)
            {
                return ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>.Erro(
                    ErpMetadadoFalha.UnidadeAlocacaoInvalida,
                    "Uma ou mais Unidades de Alocação informadas não existem nesta Unidade de Negócio.");
            }
        }

        if (input.PadraoId is Guid padraoId && !ids.Contains(padraoId))
        {
            return ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>.Erro(
                ErpMetadadoFalha.PadraoForaDoVinculo,
                "A Unidade de Alocação padrão deve estar entre as Unidades vinculadas nesta requisição.");
        }

        var agora = clock.GetUtcNow();
        var resolvido = await CentroCustoMetadadoResolver.ObterOuCriarAsync(metadados, codigo, unidadeNegocioId, agora, ct);
        if (!resolvido.Sucesso)
        {
            return ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>.Erro(resolvido.Falha, resolvido.Mensagem!);
        }

        var local = resolvido.Valor!;
        var novosVinculos = ids.Select(id => (UnidadeAlocacaoId: id, Padrao: id == input.PadraoId)).ToArray();
        await vinculos.SubstituirVinculosAsync(local.Id, novosVinculos, ct);

        try
        {
            await vinculos.SalvarAlteracoesAsync(ct);
        }
        catch (DuplicateRecordException)
        {
            return ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>.Erro(
                ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio,
                "Este Centro de Custo foi ancorado por outra requisição concorrente em outra Unidade de Negócio.");
        }

        var dtos = await ListarVinculosUnidadeAlocacaoUseCase.ProjetarAsync(vinculos, unidadesAlocacao, local.Id, unidadeNegocioId, ct);
        return ErpMetadadoResultado<IReadOnlyList<UnidadeAlocacaoVinculoDto>>.Ok(dtos);
    }
}
