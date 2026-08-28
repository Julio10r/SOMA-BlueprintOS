namespace BlueprintOS.Core.AI.Negotiation.Models;

/// <summary>
/// Representa a postura de negociação escolhida pelo agente comprador sênior para
/// conduzir a negociação com um fornecedor.
/// </summary>
public enum NegotiationStrategyType
{
    /// <summary>
    /// Postura de forte pressão por redução de preço, utilizada quando o preço atual
    /// está muito acima do histórico do produto.
    /// </summary>
    Aggressive,

    /// <summary>
    /// Postura equilibrada entre preço e relacionamento, utilizada principalmente
    /// quando ainda não há histórico consolidado com o fornecedor.
    /// </summary>
    Balanced,

    /// <summary>
    /// Postura cautelosa, priorizando a continuidade do fornecimento em detrimento
    /// de ganhos agressivos de preço.
    /// </summary>
    Conservative,

    /// <summary>
    /// Postura colaborativa de longo prazo, utilizada com fornecedores de alto score
    /// e com relação de compra recorrente.
    /// </summary>
    Partnership,

    /// <summary>
    /// Postura que explora a concorrência entre fornecedores para obter melhores
    /// condições, utilizada quando o fornecedor está caro e há alternativas no mercado.
    /// </summary>
    Competitive,

    /// <summary>
    /// Postura utilizada em compras urgentes, priorizando a garantia do fornecimento
    /// mesmo que isso implique em condições de preço menos favoráveis.
    /// </summary>
    Emergency,
}
