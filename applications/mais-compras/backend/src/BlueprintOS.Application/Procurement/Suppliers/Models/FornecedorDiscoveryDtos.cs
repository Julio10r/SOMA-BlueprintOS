namespace BlueprintOS.Application.Procurement.Suppliers.Models;

public sealed record DescobrirFornecedoresDto(string CodigoItem, string? Descricao, string? Categoria);
public sealed record FornecedorDescobertoDto(Guid Id, string CodigoItem, string? Descricao, string? Categoria,
    string Nome, string? Cnpj, string? CodigoFornecedor, decimal Score, string Criterio, Guid TemporaryUserId,
    DateTimeOffset DescobertoEm);
