using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.Infrastructure.Administration;

/// <summary>Listagem de Unidades de Medida combinando leitura real do ERP (`IUnidadeMedidaErpReader`) com
/// os metadados locais (B3 — Bloco 2). Mesmo padrão de <c>ListarContasContabeisUseCase</c>, sem a
/// restrição adicional de status ERP (que não existe para Unidade).
///
/// DECISÃO DO PRODUCT OWNER (homologação do Bloco 2, 2026-09-02): `UNIDADES` tem uma ocorrência real no
/// Linx com código nulo/vazio/só espaços ("VAZIO AUXILIAR") — unidades sem código válido não devem ser
/// disponibilizadas para uso funcional no +Compras (não aparecem na listagem/seleção, não geram metadado
/// utilizável). O Linx não é alterado — o filtro é só do lado da leitura/listagem do +Compras.</summary>
public sealed class ListarUnidadesMedidaUseCase(IUnidadeMedidaErpReader reader, IUnidadeMedidaMetadadoRepository metadados) : IListarUnidadesMedidaUseCase
{
    private const int LimiteLeitura = 5000;

    public async Task<IReadOnlyList<UnidadeMedidaDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var doErp = await reader.BuscarUnidadesAsync(0, LimiteLeitura, ct);
        var locais = await metadados.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);

        return doErp
            .Where(unidade => !string.IsNullOrWhiteSpace(unidade.CodigoErp))
            .Select(unidade => Projetar(unidade, locais.GetValueOrDefault(unidade.CodigoErp)))
            .ToArray();
    }

    private static UnidadeMedidaDto Projetar(UnidadeMedidaErpDto erp, UnidadeMedidaMetadado? local) => new(
        erp.CodigoErp,
        erp.DescricaoErp,
        local?.DescricaoMaisCompras,
        local?.AtivoNoMaisCompras ?? true,
        local is not null,
        local?.AtualizadoEm ?? erp.UltimaAlteracaoEm);
}

/// <summary>Cria o metadado local na primeira edição/ativação de uma Unidade de Medida, ou atualiza o
/// existente. Nunca cria/edita/exclui o dado ERP — apenas confirma, via <see cref="IUnidadeMedidaErpReader"/>,
/// que o código informado corresponde a uma Unidade real antes de persistir qualquer coisa localmente.</summary>
public sealed class AtualizarMetadadoUnidadeMedidaUseCase(
    IUnidadeMedidaErpReader reader,
    IUnidadeMedidaMetadadoRepository metadados,
    TimeProvider clock) : IAtualizarMetadadoUnidadeMedidaUseCase
{
    public async Task<ErpMetadadoResultado<UnidadeMedidaDto>> ExecuteAsync(
        string codigoErp, UnidadeMedidaMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var codigo = (codigoErp ?? string.Empty).Trim();

        // Defesa em profundidade (independente do leitor ERP): um código vazio/só espaços nunca é uma
        // Unidade de Medida válida para uso no +Compras — mesma decisão do Product Owner aplicada em
        // ListarUnidadesMedidaUseCase.
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return ErpMetadadoResultado<UnidadeMedidaDto>.Erro(
                ErpMetadadoFalha.CodigoErpNaoEncontrado, "Código ERP de Unidade de Medida não encontrado.");
        }

        var erp = await reader.BuscarPorCodigoAsync(codigo, ct);
        if (erp is null)
        {
            return ErpMetadadoResultado<UnidadeMedidaDto>.Erro(
                ErpMetadadoFalha.CodigoErpNaoEncontrado, "Código ERP de Unidade de Medida não encontrado.");
        }

        var agora = clock.GetUtcNow();
        var local = await metadados.ObterPorCodigoErpAsync(codigo, unidadeNegocioId, ct);
        if (local is null)
        {
            local = new UnidadeMedidaMetadado(codigo, unidadeNegocioId, agora, input.DescricaoMaisCompras, input.AtivoNoMaisCompras);
            await metadados.AdicionarAsync(local, ct);
        }
        else
        {
            local.AtualizarDescricao(input.DescricaoMaisCompras, agora);
            if (input.AtivoNoMaisCompras) local.Ativar(agora); else local.Inativar(agora);
        }

        await metadados.SalvarAlteracoesAsync(ct);

        var dto = new UnidadeMedidaDto(erp.CodigoErp, erp.DescricaoErp,
            local.DescricaoMaisCompras, local.AtivoNoMaisCompras, true, local.AtualizadoEm);
        return ErpMetadadoResultado<UnidadeMedidaDto>.Ok(dto);
    }
}
