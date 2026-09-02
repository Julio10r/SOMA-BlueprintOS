namespace BlueprintOS.Application.Identity.Models;

/// <summary>Projeção de leitura de uma Referência de Item Fiscal por Fornecedor (B3 — Bloco 4, Discovery
/// homologado). <c>FornecedorNome</c> é enriquecido a partir do cadastro de Fornecedores existente — nunca
/// duplicado localmente.</summary>
public sealed record ItemFiscalReferenciaFornecedorDto(
    Guid Id,
    Guid ItemFiscalId,
    Guid FornecedorId,
    string FornecedorNome,
    string CodigoItemFornecedor,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

/// <summary>Entrada de criação de uma referência. <c>FornecedorId</c> é imutável após a criação (mesma
/// decisão de <c>ItemFiscal.Codigo</c>) — para associar a outro fornecedor, remova e recrie.</summary>
public sealed record ItemFiscalReferenciaFornecedorCriarInput(Guid FornecedorId, string CodigoItemFornecedor);

/// <summary>Entrada de edição de uma referência. Sem <c>FornecedorId</c>: imutável após a criação.</summary>
public sealed record ItemFiscalReferenciaFornecedorAtualizarInput(string CodigoItemFornecedor);
