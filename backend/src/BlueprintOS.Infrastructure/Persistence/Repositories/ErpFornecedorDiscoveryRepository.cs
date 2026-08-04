using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>Adaptador de leitura do ERP; não cria migrations nem executa escrita em SOMA_DESENV.</summary>
public sealed class ErpFornecedorDiscoveryRepository(IConfiguration configuration) : IErpFornecedorDiscoveryRepository
{
    private static readonly string[] NameAliases = ["nome", "nome_fornecedor", "razao_social", "razaosocial", "fornecedor_nome", "fantasia", "fornecedor"];
    private static readonly string[] CnpjAliases = ["cnpj", "cpf_cnpj", "cgc_cpf", "documento"];
    private static readonly string[] SupplierCodeAliases = ["codigo_fornecedor", "cod_fornecedor", "id_fornecedor", "fornecedor_id"];
    private static readonly string[] ItemAliases = ["codigo_item", "cod_item", "codigo_produto", "cod_produto", "produto_id", "item_id"];
    private static readonly string[] DescriptionAliases = ["descricao", "descricao_item", "descricao_produto", "produto"];
    private static readonly string[] FamilyAliases = ["familia", "familia_item", "grupo", "subgrupo"];
    private static readonly string[] CategoryAliases = ["categoria", "categoria_item", "departamento", "linha"];

    public async Task<IReadOnlyList<ErpFornecedorCandidate>> DescobrirAsync(FornecedorDiscoveryQuery query, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("ErpConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal))
            throw new InvalidOperationException("ConnectionStrings:ErpConnection não está configurada.");
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, "SOMA_DESENV", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A descoberta deve usar exclusivamente o banco SOMA_DESENV.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var tables = await LoadTablesAsync(connection, cancellationToken);
        var result = new List<ErpFornecedorCandidate>();
        foreach (var table in tables)
        {
            var rows = await ReadCandidatesAsync(connection, table, query, cancellationToken);
            result.AddRange(rows);
        }
        return result.GroupBy(x => $"{x.CodigoFornecedor}|{x.Cnpj}|{x.Nome}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(x => ScoreFornecedorValue(x)).First()).ToArray();
    }

    private static decimal ScoreFornecedorValue(ErpFornecedorCandidate x) =>
        x.ItemExato ? 100 : x.Familia ? 80 : x.Categoria ? 60 : x.Historico ? 40 : 0;

    private static async Task<IReadOnlyList<TableShape>> LoadTablesAsync(SqlConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS c
            WHERE c.TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA')
            ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION;
            """;
        var groups = new Dictionary<string, TableShape>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0); var table = reader.GetString(1); var column = reader.GetString(2);
            var key = $"{schema}.{table}";
            if (!groups.TryGetValue(key, out var shape)) groups[key] = shape = new TableShape(schema, table);
            shape.Columns.Add(column);
        }
        return groups.Values.Where(x => x.NameColumn is not null && (x.CnpjColumn is not null || x.SupplierCodeColumn is not null)
            && (x.ItemColumn is not null || x.CategoryColumn is not null || x.Table.Contains("fornec", StringComparison.OrdinalIgnoreCase))).ToArray();
    }

    private static async Task<IReadOnlyList<ErpFornecedorCandidate>> ReadCandidatesAsync(SqlConnection connection, TableShape table,
        FornecedorDiscoveryQuery query, CancellationToken ct)
    {
        var name = Select(table.NameColumn); var cnpj = Select(table.CnpjColumn); var supplierCode = Select(table.SupplierCodeColumn);
        var item = Select(table.ItemColumn); var description = Select(table.DescriptionColumn); var family = Select(table.FamilyColumn); var category = Select(table.CategoryColumn);
        var searchable = new[] { table.ItemColumn, table.DescriptionColumn, table.FamilyColumn, table.CategoryColumn }.Where(x => x is not null).Select(x => Quote(x!)).ToArray();
        if (searchable.Length == 0) return [];
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT TOP (200) {name} AS Nome, {cnpj} AS Cnpj, {supplierCode} AS CodigoFornecedor, {item} AS CodigoItem, {description} AS Descricao, {family} AS Familia, {category} AS Categoria FROM {Quote(table.Schema)}.{Quote(table.Table)} WHERE {string.Join(" OR ", searchable.Select((column, i) => $"CAST({column} AS nvarchar(500)) LIKE @p{i}"))};";
        var terms = new[] { query.CodigoItem, query.Descricao, query.Categoria }.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        for (var i = 0; i < searchable.Length; i++) command.Parameters.Add(new SqlParameter($"@p{i}", $"%{terms[0]}%"));
        var result = new List<ErpFornecedorCandidate>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var itemValue = Value(reader, "CodigoItem"); var descriptionValue = Value(reader, "Descricao"); var familyValue = Value(reader, "Familia"); var categoryValue = Value(reader, "Categoria");
            var exact = string.Equals(itemValue, query.CodigoItem, StringComparison.OrdinalIgnoreCase);
            var familyMatch = !exact && (!string.IsNullOrWhiteSpace(familyValue) && !string.IsNullOrWhiteSpace(query.Descricao) && query.Descricao.Contains(familyValue, StringComparison.OrdinalIgnoreCase));
            var categoryMatch = !exact && !familyMatch && (!string.IsNullOrWhiteSpace(categoryValue) && string.Equals(categoryValue, query.Categoria, StringComparison.OrdinalIgnoreCase));
            var history = table.Table.Contains("histor", StringComparison.OrdinalIgnoreCase);
            if (exact || familyMatch || categoryMatch || history)
                result.Add(new(Value(reader, "Nome") ?? "Fornecedor sem nome", Value(reader, "Cnpj"), Value(reader, "CodigoFornecedor"), exact, familyMatch, categoryMatch, history));
        }
        return result;
    }

    private static string Select(string? column) => column is null ? "NULL" : Quote(column);
    private static string Quote(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    private static string? Value(IDataRecord reader, string name) => reader[name] is DBNull ? null : Convert.ToString(reader[name]);

    private sealed class TableShape(string schema, string table)
    {
        public string Schema { get; } = schema; public string Table { get; } = table; public List<string> Columns { get; } = [];
        public string? NameColumn => Find(NameAliases); public string? CnpjColumn => Find(CnpjAliases); public string? SupplierCodeColumn => Find(SupplierCodeAliases);
        public string? ItemColumn => Find(ItemAliases); public string? DescriptionColumn => Find(DescriptionAliases); public string? FamilyColumn => Find(FamilyAliases); public string? CategoryColumn => Find(CategoryAliases);
        private string? Find(string[] aliases) => Columns.FirstOrDefault(column => aliases.Contains(column, StringComparer.OrdinalIgnoreCase));
    }
}
