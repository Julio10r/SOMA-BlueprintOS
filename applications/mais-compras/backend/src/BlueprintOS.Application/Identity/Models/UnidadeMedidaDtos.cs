namespace BlueprintOS.Application.Identity.Models;

/// <summary>Projeção de leitura de Unidade de Medida (B3 — Bloco 2). <c>CodigoErp</c>/<c>DescricaoErp</c>
/// vêm do ERP (`UNIDADES`); <c>DescricaoMaisCompras</c>/<c>AtivoNoMaisCompras</c> são os metadados locais do
/// +Compras. Diferente de <see cref="ContaContabilDto"/>: `UNIDADES` não possui coluna de status no Linx
/// (comprovado por schema discovery dedicado), então não existe `AtivoEfetivo` distinto — o campo
/// `AtivoNoMaisCompras` já é a única fonte de verdade de ativo/inativo para Unidade. Quando não existe
/// metadado local, o registro é considerado Ativo por padrão (mesma regra de Filial/CentroCusto/Conta
/// Contábil).</summary>
public sealed record UnidadeMedidaDto(
    string CodigoErp,
    string DescricaoErp,
    string? DescricaoMaisCompras,
    bool AtivoNoMaisCompras,
    bool TemMetadadoLocal,
    DateTimeOffset? AtualizadoEm);

public sealed record UnidadeMedidaMetadadoInput(string? DescricaoMaisCompras, bool AtivoNoMaisCompras);
