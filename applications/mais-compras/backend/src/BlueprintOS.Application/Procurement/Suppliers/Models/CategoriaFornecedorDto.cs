namespace BlueprintOS.Application.Procurement.Suppliers.Models;

/// <summary>Item do catálogo pré-cadastrado de Categoria de Fornecedor (Gate de homologação,
/// 2026-09-01) — consumido pelo combobox de Categoria no cadastro/edição de Fornecedor.</summary>
public sealed record CategoriaFornecedorDto(string Codigo, string Descricao);
