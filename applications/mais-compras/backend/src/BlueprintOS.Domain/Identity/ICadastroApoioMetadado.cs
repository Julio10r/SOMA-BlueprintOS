namespace BlueprintOS.Domain.Identity;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): contrato mínimo compartilhado pelos cadastros de apoio
/// cuja origem é o Linx (<see cref="ContaContabilMetadado"/>, <see cref="UnidadeMedidaMetadado"/>,
/// <see cref="CentroCustoMetadado"/> — hoje estruturalmente idênticos) e que o pipeline governado RAW→REFINED
/// usa para aplicar a única regra que de fato precisa ser genérica entre eles: quando o Linx sinaliza que um
/// código passou a inativo, o +Compras força a inativação local (ADR-0024: em ambiguidade, Linx prevalece) —
/// nunca o inverso (o pipeline nunca chama <c>Ativar</c>: reativar é decisão exclusiva do +Compras). Um
/// código visto no RAW sem metadado local correspondente NUNCA é criado por este contrato — provisionar
/// exige uma Unidade de Negócio, que não tem origem no Linx, e inventá-la violaria a regra geral desta
/// certificação de nunca inventar dado que não veio de evidência real.
/// </summary>
public interface ICadastroApoioMetadado
{
    Guid Id { get; }

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): usada pelo pipeline governado genérico para
    /// escopar leitura de metadados existentes por Business Unit — sem isso, dois metadados de BUs
    /// diferentes com o mesmo CodigoErp colidiriam no dicionário de projeção REFINED.</summary>
    Guid UnidadeNegocioId { get; }
    string CodigoErp { get; }
    bool AtivoNoMaisCompras { get; }
    void Inativar(DateTimeOffset agora);
}
