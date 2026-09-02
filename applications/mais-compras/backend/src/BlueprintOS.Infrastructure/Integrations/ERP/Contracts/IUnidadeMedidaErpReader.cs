namespace BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

/// <summary>Leitura real (somente leitura) de Unidades de Medida do ERP `SOMA_DESENV` (B3 — Bloco 2,
/// `UNIDADES`). Mesmo padrão de <c>IContaContabilErpReader</c>: o ERP é fonte canônica e imutável — Unidade
/// é cadastro de apoio originado do Linx (Discovery B3 homologado). Nomeado `UnidadeMedida` no código
/// (nunca apenas `Unidade`) para não colidir com os conceitos já existentes `UnidadeNegocio`/
/// `UnidadeAlocacao`.
///
/// Comprovado por schema discovery dedicado (Bloco 2, instrução do Product Owner): `UNIDADES` não possui
/// nenhuma coluna de status/ativo/inativo — ao contrário de `CTB_CONTA_PLANO.INATIVA`, não há sinalização
/// de inatividade do lado Linx para Unidade. Por isso <see cref="UnidadeMedidaErpDto"/> não carrega nenhum
/// campo de inatividade ERP (nada a comprovar/inferir) — apenas `AtivoNoMaisCompras` local decide isso no
/// +Compras.</summary>
public interface IUnidadeMedidaErpReader
{
    Task<IReadOnlyList<UnidadeMedidaErpDto>> BuscarUnidadesAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<UnidadeMedidaErpDto?> BuscarPorCodigoAsync(string codigoErp, CancellationToken cancellationToken = default);
}

public sealed record UnidadeMedidaErpDto(
    string CodigoErp,
    string DescricaoErp,
    DateTimeOffset? UltimaAlteracaoEm);
