using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.Infrastructure.Administration;

/// <summary>Implementação real de <see cref="ICentroCustoVinculoValidator"/> (resolução da dívida O1.6-L2).
///
/// Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): o índice físico de
/// <see cref="CentroCustoMetadado.CodigoErp"/> deixou de ser único globalmente — duas Unidades de Negócio
/// podem ancorar o mesmo código ERP como metadados independentes (contextos independentes, nunca
/// compartilhados). A validação em tempo de execução passa a ser inteiramente escopada pela Unidade de
/// Negócio da sessão:
///
/// Para cada código ERP informado no vínculo Usuário×Centro de Custo:
/// 1. Se já existe um <see cref="CentroCustoMetadado"/> para esse código NA MESMA Unidade de Negócio da
///    sessão (<c>ObterPorCodigoErpAsync</c>, já escopado), o vínculo é aceito reaproveitando esse metadado —
///    sem nova consulta ao ERP.
/// 2. Se não existe metadado para esse código NESTA Unidade de Negócio (esteja ele ancorado em outra BU ou
///    em nenhuma), o código é validado contra o ERP real
///    (<see cref="ICentroCustoErpReader.BuscarPorCodigoAsync"/>). Inexistente no ERP → rejeitado. Existente
///    → um novo <see cref="CentroCustoMetadado"/> é criado "sob demanda", ancorado à Unidade de Negócio da
///    sessão (Ativo por padrão) — mesmo código ERP já ancorado em outra BU não impede nem é afetado por
///    esta criação (contextos independentes).
///
/// Decisão explícita (ver relatório final da O1.7): validação em tempo de execução no caso de uso, em vez de
/// FK física em `UsuariosCentrosCusto` — a FK física exigiria que o metadado já existisse ANTES do primeiro
/// vínculo, o que não é garantido (a tela de Centros de Custo só cria metadado na primeira edição/ativação
/// manual). Criar o metadado "sob demanda" aqui, no momento do vínculo, é a forma mais simples de garantir a
/// integridade sem introduzir uma ordem de operações artificial para o usuário final.
///
/// DEB-15/M2 (Gate Final pós-O1.14): este método NUNCA chama <c>SalvarAlteracoesAsync</c> — apenas rastreia
/// o novo <see cref="CentroCustoMetadado"/> via <c>AdicionarAsync</c> no mesmo <c>DbContext</c> compartilhado
/// pelo repositório de Usuário injetado no caso de uso chamador (<c>CriarUsuarioUseCase</c>/
/// <c>AtualizarUsuarioUseCase</c>). Persistir tudo em uma única chamada (mesmo padrão de
/// <c>ConcluirBootstrapUseCase</c>) garante que, se qualquer etapa falhar, nenhuma escrita — incluindo a
/// ancoragem do metadado — é persistida.</summary>
public sealed class CentroCustoVinculoValidator(
    ICentroCustoErpReader reader,
    ICentroCustoMetadadoRepository metadados,
    TimeProvider clock) : ICentroCustoVinculoValidator
{
    public async Task<RbacResultado<IReadOnlyList<string>>> ValidarEAncorarAsync(
        IReadOnlyList<string>? codigosErp, Guid unidadeNegocioId, CancellationToken ct)
    {
        var normalizados = (codigosErp ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizados.Length == 0)
        {
            return RbacResultado<IReadOnlyList<string>>.Ok(Array.Empty<string>());
        }

        var agora = clock.GetUtcNow();

        foreach (var codigo in normalizados)
        {
            var ancoraExistente = await metadados.ObterPorCodigoErpAsync(codigo, unidadeNegocioId, ct);
            if (ancoraExistente is not null)
            {
                continue;
            }

            var doErp = await reader.BuscarPorCodigoAsync(codigo, ct);
            if (doErp is null)
            {
                return RbacResultado<IReadOnlyList<string>>.Erro(
                    RbacFalha.CentroCustoInvalido,
                    $"O Centro de Custo '{codigo}' não existe no ERP.");
            }

            // Apenas rastreado no DbContext compartilhado — persistido junto com Usuario/vínculos pelo
            // SalvarAlteracoesAsync único do caso de uso chamador (ver nota de classe acima, DEB-15/M2).
            await metadados.AdicionarAsync(new CentroCustoMetadado(codigo, unidadeNegocioId, agora), ct);
        }

        return RbacResultado<IReadOnlyList<string>>.Ok(normalizados);
    }
}
