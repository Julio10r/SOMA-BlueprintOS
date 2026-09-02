using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.Infrastructure.Administration;

/// <summary>Listagem de Contas Contábeis combinando leitura real do ERP (`IContaContabilErpReader`) com os
/// metadados locais (B3 — Bloco 1). Mesmo padrão de <c>ListarFiliaisUseCase</c>/<c>ListarCentrosCustoUseCase</c>:
/// join em memória por `CodigoErp`, lote amplo (até 5000). Diferença deliberada: `AtivoEfetivo` respeita
/// `ADR-0024` (Linx prevalece) — nunca fica `true` quando `InativaNoErp` é `true`, independentemente do
/// metadado local.</summary>
public sealed class ListarContasContabeisUseCase(IContaContabilErpReader reader, IContaContabilMetadadoRepository metadados) : IListarContasContabeisUseCase
{
    private const int LimiteLeitura = 5000;

    public async Task<IReadOnlyList<ContaContabilDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var doErp = await reader.BuscarContasContabeisAsync(0, LimiteLeitura, ct);
        var locais = await metadados.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);

        return doErp
            .Select(conta => Projetar(conta, locais.GetValueOrDefault(conta.CodigoErp)))
            .ToArray();
    }

    private static ContaContabilDto Projetar(ContaContabilErpDto erp, ContaContabilMetadado? local)
    {
        var ativoNoMaisCompras = local?.AtivoNoMaisCompras ?? true;
        var ativoEfetivo = !erp.InativaNoErp && ativoNoMaisCompras;
        return new ContaContabilDto(
            erp.CodigoErp,
            erp.DescricaoErp,
            erp.InativaNoErp,
            local?.DescricaoMaisCompras,
            ativoNoMaisCompras,
            ativoEfetivo,
            local is not null,
            local?.AtualizadoEm ?? erp.UltimaAlteracaoEm);
    }
}

/// <summary>Cria o metadado local na primeira edição/ativação de uma Conta Contábil, ou atualiza o
/// existente. Nunca cria/edita/exclui o dado ERP — apenas confirma, via <see cref="IContaContabilErpReader"/>,
/// que o código informado corresponde a uma Conta Contábil real antes de persistir qualquer coisa
/// localmente.</summary>
public sealed class AtualizarMetadadoContaContabilUseCase(
    IContaContabilErpReader reader,
    IContaContabilMetadadoRepository metadados,
    TimeProvider clock) : IAtualizarMetadadoContaContabilUseCase
{
    public async Task<ErpMetadadoResultado<ContaContabilDto>> ExecuteAsync(
        string codigoErp, ContaContabilMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var codigo = (codigoErp ?? string.Empty).Trim();
        var erp = await reader.BuscarPorCodigoAsync(codigo, ct);
        if (erp is null)
        {
            return ErpMetadadoResultado<ContaContabilDto>.Erro(
                ErpMetadadoFalha.CodigoErpNaoEncontrado, "Código ERP de Conta Contábil não encontrado.");
        }

        var agora = clock.GetUtcNow();
        var local = await metadados.ObterPorCodigoErpAsync(codigo, unidadeNegocioId, ct);
        if (local is null)
        {
            local = new ContaContabilMetadado(codigo, unidadeNegocioId, agora, input.DescricaoMaisCompras, input.AtivoNoMaisCompras);
            await metadados.AdicionarAsync(local, ct);
        }
        else
        {
            local.AtualizarDescricao(input.DescricaoMaisCompras, agora);
            if (input.AtivoNoMaisCompras) local.Ativar(agora); else local.Inativar(agora);
        }

        await metadados.SalvarAlteracoesAsync(ct);

        var ativoEfetivo = !erp.InativaNoErp && local.AtivoNoMaisCompras;
        var dto = new ContaContabilDto(erp.CodigoErp, erp.DescricaoErp, erp.InativaNoErp,
            local.DescricaoMaisCompras, local.AtivoNoMaisCompras, ativoEfetivo, true, local.AtualizadoEm);
        return ErpMetadadoResultado<ContaContabilDto>.Ok(dto);
    }
}
