using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
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
        try
        {
            await using var connection = await OpenAsync(cancellationToken);
            logger.LogInformation("Consulta de fornecedor no ERP iniciada. ERP {ErpSistema}, Operação Consulta", ErpSistema);
            var shape = await LoadShapeAsync(connection, cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = TimeoutSeconds;
            command.CommandText = $"SELECT TOP (1) {shape.SelectList} FROM {shape.FromClause} WHERE {shape.IdPredicate} = @id";
            command.Parameters.Add(new SqlParameter("@id", identificador));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader, shape) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na consulta de fornecedor ERP. ERP {ErpSistema}", ErpSistema);
            throw;
        }
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
        command.CommandText = shape.IsSomaFornecedores
            ? $"UPDATE {shape.Table} SET {shape.Quote(shape.InativoColumn)} = 1{shape.TimestampUpdate} WHERE {shape.Quote(shape.IdColumn!)} = @id; UPDATE [dbo].[CADASTRO_CLI_FOR] SET [DATA_PARA_TRANSFERENCIA] = GETDATE() WHERE [COD_CLIFOR] = @id"
            : $"UPDATE {shape.Table} SET {shape.Quote(shape.InativoColumn)} = 1{shape.TimestampUpdate} WHERE {shape.Quote(shape.IdColumn!)} = @id";
        command.Parameters.Add(new SqlParameter("@id", identificador));
        logger.LogInformation("Inativação ERP executada. ERP {ErpSistema}, Tabela {Tabela}, ColunaId {ColunaId}, ColunaInativo {ColunaInativo}, Identificador externo {IdentificadorExterno}", ErpSistema, shape.Table, shape.IdColumn, shape.InativoColumn, identificador);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) throw new InvalidOperationException("Fornecedor não encontrado no ERP.");
        return await ObterAsync(identificador, cancellationToken) ?? throw new InvalidOperationException("Fornecedor não encontrado no ERP após inativação.");
    }

    private async Task<ErpFornecedorDto> EscreverAsync(ErpFornecedorParaEscrita fornecedor, bool inserir, CancellationToken ct)
    {
        logger.LogInformation("Operação de escrita de fornecedor ERP iniciada. ERP {ErpSistema}, Inserir {Inserir}", ErpSistema, inserir);
        await using var connection = await OpenAsync(ct);
        logger.LogInformation("Conexão ERP aberta. ERP {ErpSistema}", ErpSistema);
        var shape = await LoadShapeAsync(connection, ct);
        logger.LogInformation("Metadados ERP carregados. ERP {ErpSistema}, Tabela {Tabela}", ErpSistema, shape.Table);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        logger.LogInformation("Transação ERP iniciada. ERP {ErpSistema}, Inserir {Inserir}", ErpSistema, inserir);
        try
        {
            await using var command = connection.CreateCommand(); command.CommandTimeout = TimeoutSeconds; command.Transaction = (SqlTransaction)transaction;
            var externalId = fornecedor.Id;
            if (inserir && shape.IsSomaFornecedores) externalId = await NextCliforAsync(connection, command.Transaction, ct);
            if (inserir)
            {
                command.CommandText = shape.IsSomaFornecedores
                    ? $"INSERT INTO [dbo].[CADASTRO_CLI_FOR] ([NOME_CLIFOR], [CLIFOR], [COD_CLIFOR], [CGC_CPF], [RAZAO_SOCIAL], [RG_IE], [UF], [COBRANCA_UF], [ENTREGA_UF], [COBRANCA_CGC], [CADASTRAMENTO], [COBRANCA_IE], [ENTREGA_CGC], [ENTREGA_IE], [PAIS], [COBRANCA_PAIS], [ENTREGA_PAIS]{shape.CadastroTimestampInsertColumn}) VALUES (@nome, @id, @id, @cnpj, @nome, @empty, @uf, @uf, @uf, @cnpj, GETDATE(), @empty, @cnpj, @empty, @paisErp, @paisErp, @paisErp{shape.CadastroTimestampInsertValue}); INSERT INTO {shape.Table} ([COD_FORNECEDOR], [CLIFOR], [FORNECEDOR], [CONDICAO_PGTO], [CGC_CPF], [INATIVO]{shape.TimestampInsertColumn}) VALUES (@id, @id, @nome, '001', @cnpj, 0{shape.TimestampInsertValue})"
                    : $"INSERT INTO {shape.Table} ({shape.WriteColumns}) VALUES ({shape.WriteValues})";
            }
            else
            {
                command.CommandText = shape.IsSomaFornecedores
                    ? $"UPDATE [dbo].[CADASTRO_CLI_FOR] SET [CGC_CPF] = @cnpj, [COBRANCA_CGC] = @cnpj, [ENTREGA_CGC] = @cnpj{shape.CadastroTimestampUpdate} WHERE [COD_CLIFOR] = @id; UPDATE {shape.Table} SET [CGC_CPF] = @cnpj, [INATIVO] = @inativo{shape.TimestampUpdate} WHERE [COD_FORNECEDOR] = @id"
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
            await transaction.CommitAsync(ct);
            var confirmado = await ObterAsync(externalId, ct);
            if (confirmado is null) throw new InvalidOperationException("O ERP não confirmou o cadastro do fornecedor.");
            return confirmado;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na escrita transacional do fornecedor ERP. ERP {ErpSistema}, Inserir {Inserir}", ErpSistema, inserir);
            try { await transaction.RollbackAsync(CancellationToken.None); } catch (Exception rollbackError) { logger.LogWarning(rollbackError, "Falha ao desfazer transação ERP de fornecedor. ERP {ErpSistema}", ErpSistema); }
            throw;
        }
    }

    private async Task<string> NextCliforAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction; command.CommandTimeout = TimeoutSeconds;
        command.CommandText = "DECLARE @CLIFOR CHAR(6); EXEC LX_SEQUENCIAL @TABELA_COLUNA = 'FORNECEDORES.CLIFOR', @EMPRESA = 1, @SEQUENCIA = @CLIFOR OUTPUT, @UPDATE_SEQUENCIAL = 1; SELECT @CLIFOR AS CLIFOR;";
        try
        {
            var value = await command.ExecuteScalarAsync(ct);
            var clifor = Convert.ToString(value)?.Trim();
            if (string.IsNullOrWhiteSpace(clifor) || clifor.Length != 6 || clifor.Contains('*')) throw new InvalidOperationException("O mecanismo sequencial do ERP retornou um CLIFOR inválido.");
            logger.LogInformation("CLIFOR gerado pelo mecanismo sequencial do ERP. ERP {ErpSistema}, Operação Criação", ErpSistema);
            return clifor;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao executar o mecanismo sequencial do ERP. ERP {ErpSistema}", ErpSistema);
            throw;
        }
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
        await reader.DisposeAsync();
        var cadastroColumns = new List<string>();
        if (string.Equals(table, "FORNECEDORES", StringComparison.OrdinalIgnoreCase) && string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase))
        {
            await using var cadastroCommand = connection.CreateCommand(); cadastroCommand.CommandTimeout = TimeoutSeconds;
            cadastroCommand.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'CADASTRO_CLI_FOR' ORDER BY ORDINAL_POSITION";
            await using var cadastroReader = await cadastroCommand.ExecuteReaderAsync(ct);
            while (await cadastroReader.ReadAsync(ct)) cadastroColumns.Add(cadastroReader.GetString(0));
            await cadastroReader.DisposeAsync();
        }
        var shape = new TableShape(schema, table, columns, cadastroColumns);
        if (shape.IdColumn is null || shape.NameColumn is null || shape.CnpjColumn is null) throw new InvalidOperationException("Tabela de fornecedores do ERP não possui o mapeamento configurado.");
        return shape;
    }

    private static ErpFornecedorDto Map(IDataRecord reader, TableShape shape)
    {
        var canonical = shape.IsSomaFornecedores ? MapCanonical(reader) : null;
        var ativo = !string.Equals(Nullable(reader, "Ativo"), "1", StringComparison.OrdinalIgnoreCase) && !string.Equals(Nullable(reader, "Ativo"), "True", StringComparison.OrdinalIgnoreCase);
        return new(Convert.ToString(reader["Id"])!, canonical?.RazaoSocial ?? Nullable(reader, "Nome") ?? string.Empty,
            canonical?.DocumentoFiscal ?? Nullable(reader, "Cnpj"), canonical?.Cidade ?? Nullable(reader, "Cidade"), canonical?.Uf ?? Nullable(reader, "Estado"), canonical?.Pais ?? Nullable(reader, "Pais"),
            ativo, ParseDate(reader, "UltimaAlteracao"), canonical?.HashDadosSincronizaveis, canonical is null ? null : canonical with { Ativo = ativo, DataUltimaAlteracao = ParseDate(reader, "UltimaAlteracao") ?? canonical.DataUltimaAlteracao });
    }

    private static FornecedorCanonico MapCanonical(IDataRecord reader) => new(
        RazaoSocial: Nullable(reader, "CanonicalRazaoSocial") ?? Nullable(reader, "Nome") ?? string.Empty,
        NomeFantasia: Nullable(reader, "CanonicalNomeFantasia"), DocumentoFiscal: Nullable(reader, "CanonicalCnpj") ?? Nullable(reader, "Cnpj") ?? string.Empty,
        TipoPessoa: Nullable(reader, "CanonicalTipoPessoa"), Pais: Nullable(reader, "CanonicalPais"), InscricaoEstadual: Nullable(reader, "CanonicalInscricaoEstadual"), InscricaoMunicipal: null,
        Cep: Nullable(reader, "CanonicalCep"), Logradouro: Nullable(reader, "CanonicalLogradouro"), Numero: Nullable(reader, "CanonicalNumero"), Complemento: Nullable(reader, "CanonicalComplemento"), Bairro: Nullable(reader, "CanonicalBairro"),
        Cidade: Nullable(reader, "CanonicalCidade"), Uf: Nullable(reader, "CanonicalUf"), CodigoMunicipio: Nullable(reader, "CanonicalCodigoMunicipio"), Ddd: Nullable(reader, "CanonicalDdd"), Telefone: Nullable(reader, "CanonicalTelefone"),
        EmailComercial: Nullable(reader, "CanonicalEmailComercial"), EmailFiscal: Nullable(reader, "CanonicalEmailFiscal"), Banco: Nullable(reader, "CanonicalBanco"), Agencia: Nullable(reader, "CanonicalAgencia"), Conta: Nullable(reader, "CanonicalConta"), DigitosConta: null,
        CondicaoPagamento: Nullable(reader, "CanonicalCondicaoPagamento"), TipoFornecedor: Nullable(reader, "CanonicalTipoFornecedor"), SubtipoFornecedor: Nullable(reader, "CanonicalSubtipoFornecedor"), ContaContabil: Nullable(reader, "CanonicalContaContabil"), RegimeFiscal: Nullable(reader, "CanonicalRegimeFiscal"),
        SimplesNacional: BoolNullable(reader, "CanonicalSimplesNacional"), CategoriasFornecimento: Nullable(reader, "CanonicalCategorias"), ForneceMateriais: Bool(reader, "CanonicalForneceMateriais"), ForneceConsumo: Bool(reader, "CanonicalForneceConsumo"), ForneceServicos: Bool(reader, "CanonicalForneceServicos"), ForneceProdutos: Bool(reader, "CanonicalForneceProdutos"),
        Beneficiador: Bool(reader, "CanonicalBeneficiador"), Licenciado: Bool(reader, "CanonicalLicenciado"), Ativo: true,
        DataUltimaAlteracao: ParseDate(reader, "UltimaAlteracao") ?? DateTimeOffset.UtcNow, HashDadosSincronizaveis: string.Empty);
    private static string? Nullable(IDataRecord reader, string name) => reader[name] is DBNull ? null : Convert.ToString(reader[name])?.Trim();
    private static bool Bool(IDataRecord reader, string name) => ParseBool(reader[name]) == true;
    private static bool? BoolNullable(IDataRecord reader, string name) => ParseBool(reader[name]);
    private static bool? ParseBool(object value) => value is DBNull ? null : value switch { bool boolean => boolean, byte number => number != 0, short number => number != 0, int number => number != 0, long number => number != 0, _ => bool.TryParse(Convert.ToString(value), out var parsed) ? parsed : Convert.ToString(value) == "1" };
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
        public string? IdColumn => Find("codigo_fornecedor", "cod_fornecedor", "id_fornecedor", "fornecedor_id", "codigo", "id");
        public string? NameColumn => Find("nome", "nome_fornecedor", "razao_social", "razaosocial", "fantasia", "fornecedor");
        public string? CnpjColumn => Find("cnpj", "cpf_cnpj", "cgc_cpf", "documento");
        public string? CidadeColumn => Find("cidade", "municipio"); public string? EstadoColumn => Find("estado", "uf"); public string? PaisColumn => Find("pais", "país");
        public string? InativoColumn => Find("inativo", "ativo", "situacao");
        public string? UltimaAlteracaoColumn => Find(columns, "data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao", "ultima_alteracao_em");
        public string? CadastroUltimaAlteracaoColumn => Find(cadastroColumns, "data_para_transferencia", "ultima_alteracao", "updated_at", "data_alteracao", "ultima_alteracao_em");
        public string CadastroTimestampInsertColumn => CadastroUltimaAlteracaoColumn is null ? string.Empty : $", {Q(CadastroUltimaAlteracaoColumn)}";
        public string CadastroTimestampInsertValue => CadastroUltimaAlteracaoColumn is null ? string.Empty : ", GETDATE()";
        public string CadastroTimestampUpdate => CadastroUltimaAlteracaoColumn is null ? string.Empty : $", {Q(CadastroUltimaAlteracaoColumn)} = GETDATE()";
        public string TimestampInsertColumn => UltimaAlteracaoColumn is null ? string.Empty : $", {Q(UltimaAlteracaoColumn)}";
        public string TimestampInsertValue => UltimaAlteracaoColumn is null ? string.Empty : ", GETDATE()";
        public string TimestampUpdate => UltimaAlteracaoColumn is null ? string.Empty : $", {Q(UltimaAlteracaoColumn)} = GETDATE()";
        public string FromClause => IsSomaFornecedores ? $"{Table} f LEFT JOIN [dbo].[CADASTRO_CLI_FOR] c ON c.[COD_CLIFOR] = f.[CLIFOR]" : Table;
        public string IdPredicate => IsSomaFornecedores ? "f.[COD_FORNECEDOR]" : IdColumn!;
        public string SelectList => $"{Prefix(IdColumn, "f")} AS Id, {Prefix(NameColumn, "f")} AS Nome, {Select(Prefix(CnpjColumn, "f"), "Cnpj")}, {Select(Prefix(CidadeColumn, "f"), "Cidade")}, {Select(Prefix(EstadoColumn, "f"), "Estado")}, {Select(Prefix(PaisColumn, "f"), "Pais")}, {Select(Prefix(InativoColumn, "f"), "Ativo")}, {SelectTimestamp()}{(IsSomaFornecedores ? $", {CanonicalSelectList}" : string.Empty)}";
        private string CanonicalSelectList => string.Join(", ", new[]
        {
            Select(C("RAZAO_SOCIAL"), "CanonicalRazaoSocial"), Select(C("NOME_CLIFOR"), "CanonicalNomeFantasia"), Select(C("CGC_CPF"), "CanonicalCnpj"),
            Select(Case(C("PJ_PF"), "PJ", "PF"), "CanonicalTipoPessoa"), Select(C("PAIS"), "CanonicalPais"), Select(C("RG_IE"), "CanonicalInscricaoEstadual"),
            Select(C("CEP"), "CanonicalCep"), Select(C("ENDERECO"), "CanonicalLogradouro"), Select(C("NUMERO"), "CanonicalNumero"), Select(C("COMPLEMENTO"), "CanonicalComplemento"), Select(C("BAIRRO"), "CanonicalBairro"), Select(C("CIDADE"), "CanonicalCidade"), Select(C("UF"), "CanonicalUf"), Select(C("COD_MUNICIPIO_IBGE"), "CanonicalCodigoMunicipio"),
            Select(C("DDD1"), "CanonicalDdd"), Select(C("TELEFONE1"), "CanonicalTelefone"), Select(C("EMAIL"), "CanonicalEmailComercial"), Select(C("EMAIL_NFE"), "CanonicalEmailFiscal"), Select(C("BANCO"), "CanonicalBanco"), Select(C("CC_AGENCIA"), "CanonicalAgencia"), Select(C("CC_CONTA"), "CanonicalConta"),
            Select(F("CONDICAO_PGTO"), "CanonicalCondicaoPagamento"), Select(F("TIPO"), "CanonicalTipoFornecedor"), Select(F("SUBTIPO_FORNECEDOR"), "CanonicalSubtipoFornecedor"), Select(Coalesce(C("CTB_CONTA_CONTABIL"), F("CTB_CONTA_CONTABIL")), "CanonicalContaContabil"), Select(Coalesce(C("TIPO_TRIBUTACAO"), ConvertString(C("INDICADOR_FISCAL_TERCEIRO"))), "CanonicalRegimeFiscal"),
            Select(Case(C("ATIVIDADE_SIMPLES_NACIONAL"), "1", "0"), "CanonicalSimplesNacional"), Select(C("ID_CLASIF_CLIFOR"), "CanonicalCategorias"),
            Select(F("FORNECE_MATERIAIS"), "CanonicalForneceMateriais"), Select(F("FORNECE_MAT_CONSUMO"), "CanonicalForneceConsumo"), Select(F("FORNECE_OUTROS"), "CanonicalForneceServicos"), Select(F("FORNECE_PROD_ACAB"), "CanonicalForneceProdutos"),
            Select(F("BENEFICIADOR"), "CanonicalBeneficiador"), Select(F("LICENCIADO"), "CanonicalLicenciado")
        });
        public string WriteColumns => string.Join(", ", new[] { (IdColumn, "@id"), (NameColumn, "@nome"), (CnpjColumn, "@cnpj"), (CidadeColumn, "@cidade"), (EstadoColumn, "@estado"), (PaisColumn, "@pais") }.Where(x => x.Item1 is not null).Select(x => Q(x.Item1!)));
        public string WriteValues => string.Join(", ", new[] { (IdColumn, "@id"), (NameColumn, "@nome"), (CnpjColumn, "@cnpj"), (CidadeColumn, "@cidade"), (EstadoColumn, "@estado"), (PaisColumn, "@pais") }.Where(x => x.Item1 is not null).Select(x => x.Item2));
        public string UpdateSet => string.Join(", ", new[] { (NameColumn, "@nome"), (CnpjColumn, "@cnpj"), (CidadeColumn, "@cidade"), (EstadoColumn, "@estado"), (PaisColumn, "@pais") }.Where(x => x.Item1 is not null).Select(x => $"{Q(x.Item1!)} = {x.Item2}"));
        public bool IsSomaFornecedores => string.Equals(table, "FORNECEDORES", StringComparison.OrdinalIgnoreCase) && string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase);
        private string? Find(IReadOnlyList<string> source, params string[] aliases) => source.FirstOrDefault(x => aliases.Contains(x, StringComparer.OrdinalIgnoreCase));
        private string? Find(params string[] aliases) => Find(columns, aliases);
        private string? C(string column) => cadastroColumns.Contains(column, StringComparer.OrdinalIgnoreCase) ? $"c.{Q(column)}" : null;
        private string? F(string column) => columns.Contains(column, StringComparer.OrdinalIgnoreCase) ? $"f.{Q(column)}" : null;
        private static string? Coalesce(string? first, string? second) => first is null ? second : second is null ? first : $"COALESCE({first}, {second})";
        private static string? ConvertString(string? expression) => expression is null ? null : $"CONVERT(varchar(80), {expression})";
        private static string? Case(string? column, string whenTrue, string whenFalse) => column is null ? null : $"CASE WHEN {column} = 1 THEN '{whenTrue}' ELSE '{whenFalse}' END";
        private string? Prefix(string? column, string prefix) => column is null ? null : IsSomaFornecedores ? $"{prefix}.{Q(column)}" : Q(column);
        private string SelectTimestamp() => UltimaAlteracaoColumn is null && CadastroUltimaAlteracaoColumn is null ? "NULL AS UltimaAlteracao" : IsSomaFornecedores ? $"COALESCE({(CadastroUltimaAlteracaoColumn is null ? "NULL" : $"c.{Q(CadastroUltimaAlteracaoColumn)}")}, {(UltimaAlteracaoColumn is null ? "NULL" : $"f.{Q(UltimaAlteracaoColumn)}")}) AS UltimaAlteracao" : $"{Q(UltimaAlteracaoColumn!)} AS UltimaAlteracao";
        public string Quote(string value) => Q(value);
        private static string Q(string value) => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
        private static string Select(string? column, string alias) => column is null ? $"NULL AS {alias}" : $"{column} AS {alias}";
    }
}
