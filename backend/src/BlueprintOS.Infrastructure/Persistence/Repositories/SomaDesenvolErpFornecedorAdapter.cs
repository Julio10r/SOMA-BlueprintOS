using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>Adaptador isolado do SOMA_DESENV. O schema do ERP não atravessa esta fronteira.</summary>
public sealed class SomaDesenvolErpFornecedorAdapter(IConfiguration configuration, ILogger<SomaDesenvolErpFornecedorAdapter> logger) : IIntegracaoFornecedorErp
{
    public string ErpSistema => "SOMA_DESENV";

    public async Task<ErpFornecedorDto?> ObterAsync(string identificador, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        logger.LogInformation("Consulta de fornecedor no ERP iniciada. ERP {ErpSistema}, Operação Consulta", ErpSistema);
        var shape = await LoadShapeAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = $"SELECT TOP (1) {shape.SelectList} FROM {shape.Table} WHERE {shape.IdColumn} = @id";
        command.Parameters.Add(new SqlParameter("@id", identificador));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader, shape) : null;
    }

    public Task<ErpFornecedorDto> CriarAsync(ErpFornecedorParaEscrita fornecedor, CancellationToken cancellationToken = default) =>
        EscreverAsync(fornecedor, inserir: true, cancellationToken);

    public Task<ErpFornecedorDto?> ConsultarAsync(IdentificacaoFornecedorErp identificacao, CancellationToken cancellationToken = default) => ObterAsync(identificacao.IdentificadorExterno, cancellationToken);
    public Task<ErpFornecedorDto> CriarAsync(FornecedorParaErpDto fornecedor, CancellationToken cancellationToken = default) => EscreverAsync(new ErpFornecedorParaEscrita(fornecedor.Id, fornecedor.Nome, fornecedor.Cnpj, fornecedor.Cidade, fornecedor.Estado, fornecedor.Pais, fornecedor.Ativo, fornecedor.UltimaAlteracaoEm, fornecedor.HashDadosSincronizaveis, fornecedor.DadosCanonicos), inserir: true, cancellationToken);
    public Task<ErpFornecedorDto> AtualizarAsync(FornecedorParaErpDto fornecedor, CancellationToken cancellationToken = default) => EscreverAsync(new ErpFornecedorParaEscrita(fornecedor.Id, fornecedor.Nome, fornecedor.Cnpj, fornecedor.Cidade, fornecedor.Estado, fornecedor.Pais, fornecedor.Ativo, fornecedor.UltimaAlteracaoEm, fornecedor.HashDadosSincronizaveis, fornecedor.DadosCanonicos), inserir: false, cancellationToken);
    public Task<ErpFornecedorDto> InativarAsync(IdentificacaoFornecedorErp identificacao, CancellationToken cancellationToken = default) => InativarAsync(identificacao.IdentificadorExterno, cancellationToken);

    public Task<ErpFornecedorDto> AtualizarAsync(ErpFornecedorParaEscrita fornecedor, CancellationToken cancellationToken = default) =>
        EscreverAsync(fornecedor, inserir: false, cancellationToken);

    public async Task<ErpFornecedorDto> InativarAsync(string identificador, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var shape = await LoadShapeAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand(); command.CommandTimeout = TimeoutSeconds;
        if (shape.InativoColumn is null) throw new InvalidOperationException("O ERP não expõe o indicador de inativação configurado.");
        command.CommandText = $"UPDATE {shape.Table} SET {shape.Quote(shape.InativoColumn)} = 1{(shape.UltimaAlteracaoColumn is null ? string.Empty : $", {shape.Quote(shape.UltimaAlteracaoColumn)} = GETDATE()")} WHERE {shape.Quote(shape.IdColumn!)} = @id";
        command.Parameters.Add(new SqlParameter("@id", identificador));
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) throw new InvalidOperationException("Fornecedor não encontrado no ERP.");
        return await ObterAsync(identificador, cancellationToken) ?? throw new InvalidOperationException("Fornecedor não encontrado no ERP após inativação.");
    }

    private async Task<ErpFornecedorDto> EscreverAsync(ErpFornecedorParaEscrita fornecedor, bool inserir, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        var shape = await LoadShapeAsync(connection, ct);
        await using var command = connection.CreateCommand(); command.CommandTimeout = TimeoutSeconds;
        var externalId = fornecedor.Id;
        if (inserir && shape.IsSomaFornecedores) externalId = await NextSupplierIdAsync(connection, shape, ct);
        if (inserir)
        {
            command.CommandText = shape.IsSomaFornecedores
                ? $"SET XACT_ABORT ON; BEGIN TRAN; INSERT INTO [dbo].[CADASTRO_CLI_FOR] ([NOME_CLIFOR], [CLIFOR], [COD_CLIFOR], [CGC_CPF], [RAZAO_SOCIAL], [RG_IE], [UF], [COBRANCA_UF], [ENTREGA_UF], [COBRANCA_CGC], [CADASTRAMENTO], [COBRANCA_IE], [ENTREGA_CGC], [ENTREGA_IE], [PAIS], [COBRANCA_PAIS], [ENTREGA_PAIS]) VALUES (@nome, @id, @id, @cnpj, @nome, @empty, @uf, @uf, @uf, @cnpj, GETDATE(), @empty, @cnpj, @empty, @paisErp, @paisErp, @paisErp); INSERT INTO {shape.Table} ([COD_FORNECEDOR], [CLIFOR], [FORNECEDOR], [CONDICAO_PGTO], [CGC_CPF], [INATIVO]) VALUES (@id, @id, @nome, '001', @cnpj, 0); COMMIT TRAN"
                : $"INSERT INTO {shape.Table} ({shape.WriteColumns}) VALUES ({shape.WriteValues})";
        }
        else
        {
            command.CommandText = shape.IsSomaFornecedores
                ? $"SET XACT_ABORT ON; BEGIN TRAN; UPDATE [dbo].[CADASTRO_CLI_FOR] SET [CGC_CPF] = @cnpj, [COBRANCA_CGC] = @cnpj, [ENTREGA_CGC] = @cnpj WHERE [COD_CLIFOR] = @id; UPDATE {shape.Table} SET [CGC_CPF] = @cnpj, [INATIVO] = @inativo WHERE [COD_FORNECEDOR] = @id; COMMIT TRAN"
                : $"UPDATE {shape.Table} SET {shape.UpdateSet} WHERE {shape.IdColumn} = @id";
        }
        command.Parameters.Add(new SqlParameter("@id", externalId)); command.Parameters.Add(new SqlParameter("@nome", fornecedor.Nome));
        command.Parameters.Add(new SqlParameter("@cnpj", fornecedor.Cnpj)); command.Parameters.Add(new SqlParameter("@cidade", (object?)fornecedor.Cidade ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@estado", (object?)fornecedor.Estado ?? DBNull.Value)); command.Parameters.Add(new SqlParameter("@pais", (object?)fornecedor.Pais ?? DBNull.Value));
        if (shape.IsSomaFornecedores) command.Parameters.Add(new SqlParameter("@inativo", fornecedor.Ativo ? 0 : 1));
        if (inserir && shape.IsSomaFornecedores)
        {
            command.Parameters.Add(new SqlParameter("@empty", string.Empty));
            command.Parameters.Add(new SqlParameter("@uf", string.IsNullOrWhiteSpace(fornecedor.Estado) ? "SP" : fornecedor.Estado));
            command.Parameters.Add(new SqlParameter("@paisErp", "BRASIL"));
        }
        if (await command.ExecuteNonQueryAsync(ct) == 0 && !inserir) throw new InvalidOperationException("Fornecedor não encontrado no ERP.");
        return new(externalId, fornecedor.Nome, fornecedor.Cnpj, fornecedor.Cidade, fornecedor.Estado, fornecedor.Pais);
    }

    private static async Task<string> NextSupplierIdAsync(SqlConnection connection, TableShape shape, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"WITH Numbers AS (SELECT TOP (100000) 900000 + ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N FROM sys.all_objects a CROSS JOIN sys.all_objects b) SELECT TOP (1) RIGHT('000000' + CONVERT(varchar(6), N), 6) FROM Numbers WHERE N < 1000000 AND NOT EXISTS (SELECT 1 FROM {shape.Table} existing WHERE TRY_CONVERT(int, existing.[COD_FORNECEDOR]) = N) ORDER BY N";
        var value = await command.ExecuteScalarAsync(ct);
        return Convert.ToString(value)?.Trim() ?? throw new InvalidOperationException("Não foi possível gerar o identificador do fornecedor no ERP.");
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connectionString = configuration.GetConnectionString("ErpConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal)) throw new InvalidOperationException("ERP não configurado.");
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.InitialCatalog, "SOMA_DESENV", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("O adaptador exige o banco SOMA_DESENV.");
        var connection = new SqlConnection(connectionString); await connection.OpenAsync(ct); return connection;
    }

    private async Task<TableShape> LoadShapeAsync(SqlConnection connection, CancellationToken ct)
    {
        var schema = configuration["ErpIntegration:SomaDesenvol:Schema"] ?? "dbo";
        var table = configuration["ErpIntegration:SomaDesenvol:Table"] ?? "FORNECEDORES";
        await using var command = connection.CreateCommand(); command.CommandTimeout = TimeoutSeconds;
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION";
        command.Parameters.Add(new SqlParameter("@schema", schema)); command.Parameters.Add(new SqlParameter("@table", table));
        var columns = new List<string>(); await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(0));
        var shape = new TableShape(schema, table, columns);
        if (shape.IdColumn is null || shape.NameColumn is null || shape.CnpjColumn is null) throw new InvalidOperationException("Tabela de fornecedores do ERP não possui o mapeamento configurado.");
        return shape;
    }

    private static ErpFornecedorDto Map(IDataRecord reader, TableShape shape) => new(Convert.ToString(reader["Id"])!, Convert.ToString(reader["Nome"])!.Trim(), Nullable(reader, "Cnpj"), Nullable(reader, "Cidade"), Nullable(reader, "Estado"), Nullable(reader, "Pais"),
        !string.Equals(Nullable(reader, "Ativo"), "1", StringComparison.OrdinalIgnoreCase) && !string.Equals(Nullable(reader, "Ativo"), "True", StringComparison.OrdinalIgnoreCase), ParseDate(reader, "UltimaAlteracao"));
    private static string? Nullable(IDataRecord reader, string name) => reader[name] is DBNull ? null : Convert.ToString(reader[name])?.Trim();
    private static DateTimeOffset? ParseDate(IDataRecord reader, string name) => reader[name] is DBNull ? null : Convert.ToDateTime(reader[name]).ToUniversalTime();
    private int TimeoutSeconds => int.TryParse(configuration["ErpIntegration:TimeoutSeconds"], out var value) ? Math.Clamp(value, 1, 120) : 30;

    private sealed class TableShape(string schema, string table, IReadOnlyList<string> columns)
    {
        public string Table { get; } = $"[{schema.Replace("]", "]]", StringComparison.Ordinal)}].[{table.Replace("]", "]]", StringComparison.Ordinal)}]";
        public string? IdColumn => Find("codigo_fornecedor", "cod_fornecedor", "id_fornecedor", "fornecedor_id", "codigo", "id");
        public string? NameColumn => Find("nome", "nome_fornecedor", "razao_social", "razaosocial", "fantasia", "fornecedor");
        public string? CnpjColumn => Find("cnpj", "cpf_cnpj", "cgc_cpf", "documento");
        public string? CidadeColumn => Find("cidade", "municipio"); public string? EstadoColumn => Find("estado", "uf"); public string? PaisColumn => Find("pais", "país");
        public string? InativoColumn => Find("inativo", "ativo", "situacao");
        public string? UltimaAlteracaoColumn => Find("data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao");
        public string SelectList => $"{Q(IdColumn!)} AS Id, {Q(NameColumn!)} AS Nome, {Select(CnpjColumn, "Cnpj")}, {Select(CidadeColumn, "Cidade")}, {Select(EstadoColumn, "Estado")}, {Select(PaisColumn, "Pais")}, {Select(InativoColumn, "Ativo")}, {Select(UltimaAlteracaoColumn, "UltimaAlteracao")}";
        public string WriteColumns => string.Join(", ", new[] { (IdColumn, "@id"), (NameColumn, "@nome"), (CnpjColumn, "@cnpj"), (CidadeColumn, "@cidade"), (EstadoColumn, "@estado"), (PaisColumn, "@pais") }.Where(x => x.Item1 is not null).Select(x => Q(x.Item1!)));
        public string WriteValues => string.Join(", ", new[] { (IdColumn, "@id"), (NameColumn, "@nome"), (CnpjColumn, "@cnpj"), (CidadeColumn, "@cidade"), (EstadoColumn, "@estado"), (PaisColumn, "@pais") }.Where(x => x.Item1 is not null).Select(x => x.Item2));
        public string UpdateSet => string.Join(", ", new[] { (NameColumn, "@nome"), (CnpjColumn, "@cnpj"), (CidadeColumn, "@cidade"), (EstadoColumn, "@estado"), (PaisColumn, "@pais") }.Where(x => x.Item1 is not null).Select(x => $"{Q(x.Item1!)} = {x.Item2}"));
        public bool IsSomaFornecedores => string.Equals(table, "FORNECEDORES", StringComparison.OrdinalIgnoreCase) && string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase);
        private string? Find(params string[] aliases) => columns.FirstOrDefault(x => aliases.Contains(x, StringComparer.OrdinalIgnoreCase));
        public string Quote(string value) => Q(value);
        private static string Q(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
        private static string Select(string? column, string alias) => column is null ? $"NULL AS {alias}" : $"{Q(column)} AS {alias}";
    }
}
