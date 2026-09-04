using System.Data;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>Leitura real de Item Fiscal do SOMA_DESENV/SOMA, mesmo padrão de <c>SomaContaContabilReader</c>:
/// introspecção dinâmica via INFORMATION_SCHEMA.COLUMNS com resolução de aliases candidatos. Colunas reais
/// confirmadas em `CADASTRO_ITEM_FISCAL` (pré-validação real da B3, `docs/audits/B3-Bloco5A-
/// PreValidacaoLinxProducao.md`): `CODIGO_ITEM`/`ITEM_DESCRICAO`/`UNIDADE`/`INATIVO` `NOT NULL`;
/// `CONTA_CONTABIL`/`DATA_PARA_TRANSFERENCIA` `NULLABLE`. Por padrão lê de `CADASTRO_ITEM_FISCAL`,
/// configurável via `ErpIntegration:SomaDesenvol:ItensFiscaisSchema`/`ItensFiscaisTable`.</summary>
public sealed class SomaItemFiscalReader(IConfiguration configuration, ILogger<SomaItemFiscalReader> logger) : IItemFiscalErpReader
{
    public async Task<IReadOnlyList<ItemFiscalErpDto>> BuscarItensFiscaisAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var offset = Math.Max(0, skip);
        var pageSize = Math.Clamp(take <= 0 ? 500 : take, 1, 5000);
        await using var connection = await OpenAsync(cancellationToken);
        var shape = await LoadShapeAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = $"SELECT {shape.SelectList} FROM {shape.Table} WHERE {shape.CodigoColumnQ} IS NOT NULL ORDER BY {shape.CodigoColumnQ} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
        command.Parameters.Add(new SqlParameter("@skip", SqlDbType.Int) { Value = offset });
        command.Parameters.Add(new SqlParameter("@take", SqlDbType.Int) { Value = pageSize });

        logger.LogInformation("Leitura operacional de itens fiscais SOMA iniciada. Skip {Skip}. Take {Take}", offset, pageSize);
        var itens = new List<ItemFiscalErpDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) itens.Add(Map(reader));
        return itens;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connectionString = LinxConnectionStringResolver.Resolve(configuration, LinxConnectionProfiles.Development);
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private async Task<TableShape> LoadShapeAsync(SqlConnection connection, CancellationToken ct)
    {
        var schema = configuration["ErpIntegration:SomaDesenvol:ItensFiscaisSchema"] ?? "dbo";
        var table = configuration["ErpIntegration:SomaDesenvol:ItensFiscaisTable"] ?? "CADASTRO_ITEM_FISCAL";
        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION";
        command.Parameters.Add(new SqlParameter("@schema", schema));
        command.Parameters.Add(new SqlParameter("@table", table));
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(0));

        var shape = new TableShape(schema, table, columns);
        if (shape.CodigoColumn is null || shape.DescricaoColumn is null)
            throw new InvalidOperationException("Tabela de itens fiscais do ERP não possui colunas mínimas (código/descrição) configuradas.");
        return shape;
    }

    private static ItemFiscalErpDto Map(IDataRecord reader) => new(
        Convert.ToString(reader["CodigoItem"])!.Trim(),
        Convert.ToString(reader["Descricao"])?.Trim() ?? string.Empty,
        NullableTrim(reader, "UnidadeErp"),
        NullableTrim(reader, "ContaContabilErp"),
        reader["Inativo"] is not DBNull && Convert.ToBoolean(reader["Inativo"]),
        ParseDate(reader, "UltimaAlteracaoEm"));

    private static string? NullableTrim(IDataRecord reader, string name)
    {
        if (reader[name] is DBNull) return null;
        var valor = Convert.ToString(reader[name])?.Trim();
        return string.IsNullOrEmpty(valor) ? null : valor;
    }

    private static DateTimeOffset? ParseDate(IDataRecord reader, string name)
    {
        if (reader[name] is DBNull) return null;
        var local = DateTime.SpecifyKind(Convert.ToDateTime(reader[name]), DateTimeKind.Unspecified);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone));
    }

    private int TimeoutSeconds => int.TryParse(configuration["ErpIntegration:TimeoutSeconds"], out var value) ? Math.Clamp(value, 1, 120) : 30;

    private sealed class TableShape(string schema, string table, IReadOnlyList<string> columns)
    {
        public string Table { get; } = $"[{schema.Replace("]", "]]", StringComparison.Ordinal)}].[{table.Replace("]", "]]", StringComparison.Ordinal)}]";
        // Nomes reais confirmados em CADASTRO_ITEM_FISCAL pela pré-validação real da B3 (Produção,
        // 02/09/2026) — demais candidatos mantidos por tolerância a variações de ambiente, mesmo padrão
        // dos demais readers.
        public string? CodigoColumn => Find(columns, "codigo_item", "cod_item", "codigo");
        public string? DescricaoColumn => Find(columns, "item_descricao", "descricao");
        public string? UnidadeColumn => Find(columns, "unidade");
        public string? ContaContabilColumn => Find(columns, "conta_contabil");
        public string? InativoColumn => Find(columns, "inativo", "ativo");
        public string? UltimaAlteracaoColumn => Find(columns, "data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao", "ultima_alteracao_em");
        public string CodigoColumnQ => Q(CodigoColumn!);
        public string SelectList => string.Join(", ", new[]
        {
            $"{Q(CodigoColumn!)} AS CodigoItem",
            $"{Q(DescricaoColumn!)} AS Descricao",
            Select(UnidadeColumn, "UnidadeErp"),
            Select(ContaContabilColumn, "ContaContabilErp"),
            Select(InativoColumn, "Inativo"),
            Select(UltimaAlteracaoColumn, "UltimaAlteracaoEm"),
        });
        private static string? Find(IReadOnlyList<string> source, params string[] aliases) => source.FirstOrDefault(x => aliases.Contains(x, StringComparer.OrdinalIgnoreCase));
        private static string Select(string? column, string alias) => column is null ? $"NULL AS {alias}" : $"{Q(column)} AS {alias}";
        private static string Q(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
