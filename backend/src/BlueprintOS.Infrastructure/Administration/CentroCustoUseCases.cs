using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.Infrastructure.Administration;

/// <summary>Resolve o resumo do vínculo N:N Centro de Custo × Unidade de Alocação (O1.9) para projeção em
/// <see cref="CentroCustoDto"/>, sem N+1: uma única consulta de vínculos + uma única consulta de nomes de
/// Unidade de Alocação por lote de Centros de Custo.</summary>
internal static class VinculoUnidadeAlocacaoResumo
{
    public static async Task<IReadOnlyDictionary<Guid, (string? PadraoNome, int Quantidade)>> ObterAsync(
        ICentroCustoUnidadeAlocacaoRepository vinculos, IUnidadeAlocacaoRepository unidadesAlocacao,
        IReadOnlyCollection<Guid> centroCustoMetadadoIds, Guid unidadeNegocioId, CancellationToken ct)
    {
        if (centroCustoMetadadoIds.Count == 0) return new Dictionary<Guid, (string?, int)>();

        var porCentroCusto = await vinculos.ListarPorCentrosCustoMetadadoAsync(centroCustoMetadadoIds, ct);
        var todosUnidadeAlocacaoIds = porCentroCusto.Values.SelectMany(v => v).Select(v => v.UnidadeAlocacaoId).Distinct().ToArray();
        var unidades = (await unidadesAlocacao.ObterPorIdsEUnidadeNegocioAsync(todosUnidadeAlocacaoIds, unidadeNegocioId, ct))
            .ToDictionary(u => u.Id, u => u.Nome);

        return porCentroCusto.ToDictionary(
            par => par.Key,
            par =>
            {
                var padrao = par.Value.FirstOrDefault(v => v.Padrao);
                var padraoNome = padrao is not null && unidades.TryGetValue(padrao.UnidadeAlocacaoId, out var nome) ? nome : null;
                return (padraoNome, par.Value.Count);
            });
    }

    public static async Task<(string? PadraoNome, int Quantidade)> ObterUmAsync(
        ICentroCustoUnidadeAlocacaoRepository vinculos, IUnidadeAlocacaoRepository unidadesAlocacao,
        Guid centroCustoMetadadoId, Guid unidadeNegocioId, CancellationToken ct)
    {
        var resumo = await ObterAsync(vinculos, unidadesAlocacao, [centroCustoMetadadoId], unidadeNegocioId, ct);
        return resumo.GetValueOrDefault(centroCustoMetadadoId, (null, 0));
    }
}

/// <summary>Listagem de Centros de Custo combinando leitura real do ERP (`ICentroCustoErpReader`) com os
/// metadados locais já existentes (O1.7) e o resumo do vínculo N:N com Unidade de Alocação (O1.9). Mesmo
/// padrão/mesma decisão de "ativo por padrão sem metadado local" de <c>ListarFiliaisUseCase</c> — ver
/// relatório final da O1.7.</summary>
public sealed class ListarCentrosCustoUseCase(
    ICentroCustoErpReader reader,
    ICentroCustoMetadadoRepository metadados,
    ICentroCustoUnidadeAlocacaoRepository vinculos,
    IUnidadeAlocacaoRepository unidadesAlocacao) : IListarCentrosCustoUseCase
{
    private const int LimiteLeitura = 5000;

    public async Task<IReadOnlyList<CentroCustoDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var doErp = await reader.BuscarCentrosCustoAsync(0, LimiteLeitura, ct);
        var locais = await metadados.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);

        var metadadoIds = locais.Values.Select(x => x.Id).ToArray();
        var resumos = await VinculoUnidadeAlocacaoResumo.ObterAsync(vinculos, unidadesAlocacao, metadadoIds, unidadeNegocioId, ct);

        return doErp
            .Select(centro => Projetar(centro, locais.GetValueOrDefault(centro.CodigoErp), resumos))
            .ToArray();
    }

    private static CentroCustoDto Projetar(
        CentroCustoErpDto erp, CentroCustoMetadado? local,
        IReadOnlyDictionary<Guid, (string? PadraoNome, int Quantidade)> resumos)
    {
        var resumo = local is not null ? resumos.GetValueOrDefault(local.Id, (null, 0)) : (PadraoNome: (string?)null, Quantidade: 0);
        return new CentroCustoDto(
            erp.CodigoErp,
            erp.DescricaoErp,
            local?.DescricaoMaisCompras,
            local?.AtivoNoMaisCompras ?? true,
            local is not null,
            local?.AtualizadoEm ?? erp.UltimaAlteracaoEm,
            resumo.PadraoNome,
            resumo.Quantidade);
    }
}

/// <summary>Resolve o <see cref="CentroCustoMetadado"/> local de um código ERP, criando-o "sob demanda"
/// quando ainda não existir — mesma decisão da O1.7 (evita exigir uma ordem de operações artificial ao
/// usuário final). Reaproveitado por <see cref="AtualizarMetadadoCentroCustoUseCase"/> e pelo vínculo N:N
/// com Unidade de Alocação (O1.9), para nunca duplicar a lógica de ancoragem/anti-cross-BU.</summary>
internal static class CentroCustoMetadadoResolver
{
    public static async Task<ErpMetadadoResultado<CentroCustoMetadado>> ObterOuCriarAsync(
        ICentroCustoMetadadoRepository metadados, string codigo, Guid unidadeNegocioId, DateTimeOffset agora, CancellationToken ct)
    {
        var local = await metadados.ObterPorCodigoErpAsync(codigo, unidadeNegocioId, ct);
        if (local is not null) return ErpMetadadoResultado<CentroCustoMetadado>.Ok(local);

        // CentrosCustoMetadados.CodigoErp é globalmente único: um Centro de Custo só pode estar ancorado a
        // UMA Unidade de Negócio por vez (mesma regra do vínculo Usuário×Centro de Custo, O1.6-L2).
        var ancoraExistente = await metadados.ObterPorCodigoErpGlobalAsync(codigo, ct);
        if (ancoraExistente is not null && ancoraExistente.UnidadeNegocioId != unidadeNegocioId)
        {
            return ErpMetadadoResultado<CentroCustoMetadado>.Erro(
                ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio,
                "Este Centro de Custo já está ancorado a outra Unidade de Negócio.");
        }

        local = new CentroCustoMetadado(codigo, unidadeNegocioId, agora);
        await metadados.AdicionarAsync(local, ct);
        return ErpMetadadoResultado<CentroCustoMetadado>.Ok(local);
    }
}

/// <summary>Cria o metadado local na primeira edição/ativação de um Centro de Custo, ou atualiza o
/// existente. Nunca cria/edita/exclui o dado ERP — apenas confirma, via <see cref="ICentroCustoErpReader"/>,
/// que o código informado corresponde a um Centro de Custo real antes de persistir qualquer coisa
/// localmente.</summary>
public sealed class AtualizarMetadadoCentroCustoUseCase(
    ICentroCustoErpReader reader,
    ICentroCustoMetadadoRepository metadados,
    ICentroCustoUnidadeAlocacaoRepository vinculos,
    IUnidadeAlocacaoRepository unidadesAlocacao,
    TimeProvider clock) : IAtualizarMetadadoCentroCustoUseCase
{
    public async Task<ErpMetadadoResultado<CentroCustoDto>> ExecuteAsync(
        string codigoErp, CentroCustoMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var codigo = (codigoErp ?? string.Empty).Trim();
        var erp = await reader.BuscarPorCodigoAsync(codigo, ct);
        if (erp is null)
        {
            return ErpMetadadoResultado<CentroCustoDto>.Erro(
                ErpMetadadoFalha.CodigoErpNaoEncontrado, "Código ERP de Centro de Custo não encontrado.");
        }

        var agora = clock.GetUtcNow();
        var local = await metadados.ObterPorCodigoErpAsync(codigo, unidadeNegocioId, ct);
        if (local is null)
        {
            // CentrosCustoMetadados.CodigoErp é globalmente único (ver CentroCustoMetadadoConfiguration):
            // um Centro de Custo só pode estar ancorado a UMA Unidade de Negócio por vez (mesma regra do
            // vínculo Usuário×Centro de Custo, O1.6-L2). Sem esta verificação, a criação abaixo violaria
            // o índice único e vazaria um DbUpdateException não tratado para o cliente.
            var ancoraExistente = await metadados.ObterPorCodigoErpGlobalAsync(codigo, ct);
            if (ancoraExistente is not null && ancoraExistente.UnidadeNegocioId != unidadeNegocioId)
            {
                return ErpMetadadoResultado<CentroCustoDto>.Erro(
                    ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio,
                    "Este Centro de Custo já está ancorado a outra Unidade de Negócio.");
            }

            local = new CentroCustoMetadado(codigo, unidadeNegocioId, agora, input.DescricaoMaisCompras, input.AtivoNoMaisCompras);
            await metadados.AdicionarAsync(local, ct);
        }
        else
        {
            local.AtualizarDescricao(input.DescricaoMaisCompras, agora);
            if (input.AtivoNoMaisCompras) local.Ativar(agora); else local.Inativar(agora);
        }

        try
        {
            await metadados.SalvarAlteracoesAsync(ct);
        }
        catch (DuplicateRecordException)
        {
            return ErpMetadadoResultado<CentroCustoDto>.Erro(
                ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio,
                "Este Centro de Custo foi ancorado por outra requisição concorrente em outra Unidade de Negócio.");
        }

        var resumo = await VinculoUnidadeAlocacaoResumo.ObterUmAsync(vinculos, unidadesAlocacao, local.Id, unidadeNegocioId, ct);
        var dto = new CentroCustoDto(erp.CodigoErp, erp.DescricaoErp,
            local.DescricaoMaisCompras, local.AtivoNoMaisCompras, true, local.AtualizadoEm,
            resumo.PadraoNome, resumo.Quantidade);
        return ErpMetadadoResultado<CentroCustoDto>.Ok(dto);
    }
}
