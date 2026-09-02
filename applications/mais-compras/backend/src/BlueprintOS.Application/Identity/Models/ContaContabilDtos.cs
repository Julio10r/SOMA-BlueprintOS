namespace BlueprintOS.Application.Identity.Models;

/// <summary>Projeção de leitura de Conta Contábil (B3 — Bloco 1). <c>CodigoErp</c>/<c>DescricaoErp</c>/
/// <c>InativaNoErp</c> vêm do ERP (`CTB_CONTA_PLANO`); <c>DescricaoMaisCompras</c>/<c>AtivoNoMaisCompras</c>
/// são os metadados locais do +Compras. <c>AtivoEfetivo</c> aplica `ADR-0024` (Linx prevalece): uma conta
/// inativa no Linx nunca aparece efetivamente ativa no +Compras, mesmo que o metadado local diga o
/// contrário — o +Compras só pode ser MAIS restritivo que o Linx (inativar localmente uma conta que o Linx
/// ainda considera ativa), nunca menos. Quando não existe metadado local, o registro é considerado Ativo
/// por padrão do lado +Compras (mesma regra de Filial/CentroCusto), sujeito à mesma restrição do Linx.</summary>
public sealed record ContaContabilDto(
    string CodigoErp,
    string DescricaoErp,
    bool InativaNoErp,
    string? DescricaoMaisCompras,
    bool AtivoNoMaisCompras,
    bool AtivoEfetivo,
    bool TemMetadadoLocal,
    DateTimeOffset? AtualizadoEm);

public sealed record ContaContabilMetadadoInput(string? DescricaoMaisCompras, bool AtivoNoMaisCompras);
