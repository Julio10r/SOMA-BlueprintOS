using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed class SomaFornecedorReader(IConfiguration configuration, ILogger<SomaFornecedorReader> logger) : IFornecedorErpReader
{
    public async Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(int limite, CancellationToken cancellationToken = default)
        => await BuscarFornecedoresAsync(0, limite, cancellationToken);

    public async Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var offset = Math.Max(0, skip);
        var pageSize = Math.Clamp(take <= 0 ? 100 : take, 1, 5000);
        await using var connection = await OpenAsync(cancellationToken);
        var shape = await LoadShapeAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = $"SELECT {shape.SelectList} FROM {shape.FromClause} WHERE {shape.CnpjPredicate} IS NOT NULL ORDER BY {shape.OrderBy} OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
        command.Parameters.Add(new SqlParameter("@skip", SqlDbType.Int) { Value = offset });
        command.Parameters.Add(new SqlParameter("@take", SqlDbType.Int) { Value = pageSize });

        logger.LogInformation("Leitura operacional de fornecedores SOMA iniciada. Skip {Skip}. Take {Take}", offset, pageSize);
        var fornecedores = new List<FornecedorErpIntegracaoDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) fornecedores.Add(Map(reader));
        return fornecedores;
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
        var schema = configuration["ErpIntegration:SomaDesenvol:Schema"] ?? "dbo";
        var table = configuration["ErpIntegration:SomaDesenvol:Table"] ?? "FORNECEDORES";
        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION";
        command.Parameters.Add(new SqlParameter("@schema", schema));
        command.Parameters.Add(new SqlParameter("@table", table));
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(0));
        await reader.DisposeAsync();

        var cadastroColumns = new List<string>();
        if (string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase) && string.Equals(table, "FORNECEDORES", StringComparison.OrdinalIgnoreCase))
        {
            await using var cadastroCommand = connection.CreateCommand();
            cadastroCommand.CommandTimeout = TimeoutSeconds;
            cadastroCommand.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'CADASTRO_CLI_FOR' ORDER BY ORDINAL_POSITION";
            await using var cadastroReader = await cadastroCommand.ExecuteReaderAsync(ct);
            while (await cadastroReader.ReadAsync(ct)) cadastroColumns.Add(cadastroReader.GetString(0));
        }

        var shape = new TableShape(schema, table, columns, cadastroColumns);
        if (shape.IdColumn is null || shape.NameColumn is null || shape.CnpjColumn is null)
            throw new InvalidOperationException("Tabela de fornecedores do ERP não possui colunas mínimas configuradas.");
        return shape;
    }

    private static FornecedorErpIntegracaoDto Map(IDataRecord reader)
    {
        var ultimaAlteracao = ParseDate(reader, "UltimaAlteracao") ?? DateTimeOffset.UtcNow;
        var ativo = !Bool(reader, "Inativo");
        var dados = new FornecedorCanonico(
            RazaoSocial: Nullable(reader, "RazaoSocial") ?? Nullable(reader, "NomeFantasia") ?? string.Empty,
            NomeFantasia: Nullable(reader, "NomeFantasia"),
            DocumentoFiscal: Nullable(reader, "DocumentoFiscal") ?? string.Empty,
            TipoPessoa: Nullable(reader, "TipoPessoa") ?? "PJ",
            Pais: Nullable(reader, "Pais"),
            InscricaoEstadual: Nullable(reader, "InscricaoEstadual"),
            InscricaoMunicipal: null,
            Cep: Nullable(reader, "Cep"),
            Logradouro: Nullable(reader, "Logradouro"),
            Numero: Nullable(reader, "Numero"),
            Complemento: Nullable(reader, "Complemento"),
            Bairro: Nullable(reader, "Bairro"),
            Cidade: Nullable(reader, "Cidade"),
            Uf: Nullable(reader, "Uf"),
            CodigoMunicipio: Nullable(reader, "CodigoMunicipio"),
            Ddd: Nullable(reader, "Ddd"),
            Telefone: Nullable(reader, "Telefone"),
            EmailComercial: Nullable(reader, "EmailComercial"),
            EmailFiscal: Nullable(reader, "EmailFiscal"),
            Banco: Nullable(reader, "Banco"),
            Agencia: Nullable(reader, "Agencia"),
            Conta: Nullable(reader, "Conta"),
            DigitosConta: null,
            CondicaoPagamento: Nullable(reader, "CondicaoPagamento"),
            TipoFornecedor: Nullable(reader, "TipoFornecedor"),
            SubtipoFornecedor: Nullable(reader, "SubtipoFornecedor"),
            ContaContabil: Nullable(reader, "ContaContabil"),
            RegimeFiscal: Nullable(reader, "RegimeFiscal"),
            SimplesNacional: BoolNullable(reader, "SimplesNacional"),
            CategoriasFornecimento: Nullable(reader, "CategoriasFornecimento"),
            ForneceMateriais: Bool(reader, "ForneceMateriais"),
            ForneceConsumo: Bool(reader, "ForneceConsumo"),
            ForneceServicos: Bool(reader, "ForneceServicos"),
            ForneceProdutos: Bool(reader, "ForneceProdutos"),
            Beneficiador: Bool(reader, "Beneficiador"),
            Licenciado: Bool(reader, "Licenciado"),
            Ativo: ativo,
            DataUltimaAlteracao: ultimaAlteracao,
            HashDadosSincronizaveis: string.Empty);

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dados with { HashDadosSincronizaveis = string.Empty }))));
        return new(Convert.ToString(reader["ErpFornecedorId"])!.Trim(), "SOMA_DESENV", dados with { HashDadosSincronizaveis = hash }, ultimaAlteracao);
    }

    private static string? Nullable(IDataRecord reader, string name) => reader[name] is DBNull ? null : Convert.ToString(reader[name])?.Trim();
    private static bool Bool(IDataRecord reader, string name) => ParseBool(reader[name]) == true;
    private static bool? BoolNullable(IDataRecord reader, string name) => ParseBool(reader[name]);
    private static bool? ParseBool(object value) => value is DBNull ? null : value switch { bool b => b, byte n => n != 0, short n => n != 0, int n => n != 0, long n => n != 0, _ => bool.TryParse(Convert.ToString(value), out var parsed) ? parsed : Convert.ToString(value) == "1" };
    private static DateTimeOffset? ParseDate(IDataRecord reader, string name)
    {
        if (reader[name] is DBNull) return null;
        var local = DateTime.SpecifyKind(Convert.ToDateTime(reader[name]), DateTimeKind.Unspecified);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone));
    }

    private int TimeoutSeconds => int.TryParse(configuration["ErpIntegration:TimeoutSeconds"], out var value) ? Math.Clamp(value, 1, 120) : 30;

    private sealed class TableShape(string schema, string table, IReadOnlyList<string> columns, IReadOnlyList<string> cadastroColumns)
    {
        public string Table { get; } = $"[{schema.Replace("]", "]]", StringComparison.Ordinal)}].[{table.Replace("]", "]]", StringComparison.Ordinal)}]";
        public string? IdColumn => Find(columns, "codigo_fornecedor", "cod_fornecedor", "id_fornecedor", "fornecedor_id", "codigo", "id");
        public string? NameColumn => Find(columns, "nome", "nome_fornecedor", "razao_social", "razaosocial", "fantasia", "fornecedor");
        public string? CnpjColumn => Find(columns, "cnpj", "cpf_cnpj", "cgc_cpf", "documento");
        public string? InativoColumn => Find(columns, "inativo", "ativo", "situacao");
        public string? UltimaAlteracaoColumn => Find(columns, "data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao", "ultima_alteracao_em");
        public string? CadastroUltimaAlteracaoColumn => Find(cadastroColumns, "data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao", "ultima_alteracao_em");
        public bool IsSomaFornecedores => string.Equals(table, "FORNECEDORES", StringComparison.OrdinalIgnoreCase) && string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase);
        public string FromClause => IsSomaFornecedores ? $"{Table} f LEFT JOIN [dbo].[CADASTRO_CLI_FOR] c ON c.[COD_CLIFOR] = f.[CLIFOR]" : Table;
        public string CnpjPredicate => IsSomaFornecedores ? "COALESCE(c.[CGC_CPF], f.[CGC_CPF])" : Q(CnpjColumn!);
        public string OrderBy => IsSomaFornecedores ? "f.[COD_FORNECEDOR]" : Q(IdColumn!);
        public string SelectList => IsSomaFornecedores ? SomaSelectList : GenericSelectList;
        private string GenericSelectList => string.Join(", ", new[]
        {
            $"{Q(IdColumn!)} AS ErpFornecedorId", $"{Q(NameColumn!)} AS RazaoSocial", "NULL AS NomeFantasia", $"{Q(CnpjColumn!)} AS DocumentoFiscal",
            "NULL AS TipoPessoa", "NULL AS Pais", "NULL AS InscricaoEstadual", "NULL AS Cep", "NULL AS Logradouro", "NULL AS Numero",
            "NULL AS Complemento", "NULL AS Bairro", Select(Find(columns, "cidade", "municipio"), "Cidade"), Select(Find(columns, "estado", "uf"), "Uf"),
            "NULL AS CodigoMunicipio", "NULL AS Ddd", "NULL AS Telefone", "NULL AS EmailComercial", "NULL AS EmailFiscal", "NULL AS Banco",
            "NULL AS Agencia", "NULL AS Conta", "NULL AS CondicaoPagamento", "NULL AS TipoFornecedor", "NULL AS SubtipoFornecedor",
            "NULL AS ContaContabil", "NULL AS RegimeFiscal", "NULL AS SimplesNacional", "NULL AS CategoriasFornecimento",
            "NULL AS ForneceMateriais", "NULL AS ForneceConsumo", "NULL AS ForneceServicos", "NULL AS ForneceProdutos",
            "NULL AS Beneficiador", "NULL AS Licenciado", Select(InativoColumn, "Inativo"), Select(UltimaAlteracaoColumn, "UltimaAlteracao")
        });
        private string SomaSelectList => string.Join(", ", new[]
        {
            "f.[COD_FORNECEDOR] AS ErpFornecedorId", Select(Coalesce(C("RAZAO_SOCIAL"), F("FORNECEDOR")), "RazaoSocial"),
            Select(C("NOME_CLIFOR"), "NomeFantasia"), Select(Coalesce(C("CGC_CPF"), F("CGC_CPF")), "DocumentoFiscal"),
            Select(Case(C("PJ_PF"), "PJ", "PF"), "TipoPessoa"), Select(C("PAIS"), "Pais"), Select(C("RG_IE"), "InscricaoEstadual"),
            Select(C("CEP"), "Cep"), Select(C("ENDERECO"), "Logradouro"), Select(C("NUMERO"), "Numero"), Select(C("COMPLEMENTO"), "Complemento"),
            Select(C("BAIRRO"), "Bairro"), Select(C("CIDADE"), "Cidade"), Select(C("UF"), "Uf"), Select(C("COD_MUNICIPIO_IBGE"), "CodigoMunicipio"),
            Select(C("DDD1"), "Ddd"), Select(C("TELEFONE1"), "Telefone"), Select(C("EMAIL"), "EmailComercial"), Select(C("EMAIL_NFE"), "EmailFiscal"),
            Select(C("BANCO"), "Banco"), Select(C("CC_AGENCIA"), "Agencia"), Select(C("CC_CONTA"), "Conta"), Select(F("CONDICAO_PGTO"), "CondicaoPagamento"),
            Select(F("TIPO"), "TipoFornecedor"), Select(F("SUBTIPO_FORNECEDOR"), "SubtipoFornecedor"), Select(Coalesce(C("CTB_CONTA_CONTABIL"), F("CTB_CONTA_CONTABIL")), "ContaContabil"),
            Select(Coalesce(C("TIPO_TRIBUTACAO"), ConvertString(C("INDICADOR_FISCAL_TERCEIRO"))), "RegimeFiscal"), Select(Case(C("ATIVIDADE_SIMPLES_NACIONAL"), "1", "0"), "SimplesNacional"),
            Select(C("ID_CLASIF_CLIFOR"), "CategoriasFornecimento"), Select(F("FORNECE_MATERIAIS"), "ForneceMateriais"), Select(F("FORNECE_MAT_CONSUMO"), "ForneceConsumo"),
            Select(F("FORNECE_OUTROS"), "ForneceServicos"), Select(F("FORNECE_PROD_ACAB"), "ForneceProdutos"), Select(F("BENEFICIADOR"), "Beneficiador"),
            Select(F("LICENCIADO"), "Licenciado"), Select(F("INATIVO"), "Inativo"), SelectTimestamp()
        });
        private string SelectTimestamp() => UltimaAlteracaoColumn is null && CadastroUltimaAlteracaoColumn is null ? "NULL AS UltimaAlteracao" : $"COALESCE({(CadastroUltimaAlteracaoColumn is null ? "NULL" : $"c.{Q(CadastroUltimaAlteracaoColumn)}")}, {(UltimaAlteracaoColumn is null ? "NULL" : $"f.{Q(UltimaAlteracaoColumn)}")}) AS UltimaAlteracao";
        private string? C(string column) => cadastroColumns.Contains(column, StringComparer.OrdinalIgnoreCase) ? $"c.{Q(column)}" : null;
        private string? F(string column) => columns.Contains(column, StringComparer.OrdinalIgnoreCase) ? $"f.{Q(column)}" : null;
        private static string? Find(IReadOnlyList<string> source, params string[] aliases) => source.FirstOrDefault(x => aliases.Contains(x, StringComparer.OrdinalIgnoreCase));
        private static string? Coalesce(string? first, string? second) => first is null ? second : second is null ? first : $"COALESCE({first}, {second})";
        private static string? ConvertString(string? expression) => expression is null ? null : $"CONVERT(varchar(80), {expression})";
        private static string? Case(string? column, string whenTrue, string whenFalse) => column is null ? null : $"CASE WHEN {column} = 1 THEN '{whenTrue}' ELSE '{whenFalse}' END";
        private static string Select(string? expression, string alias) => expression is null ? $"NULL AS {alias}" : $"{expression} AS {alias}";
        private static string Q(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
