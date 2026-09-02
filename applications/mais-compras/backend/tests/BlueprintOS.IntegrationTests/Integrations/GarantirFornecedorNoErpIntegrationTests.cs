using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace BlueprintOS.IntegrationTests.Integrations;

/// <summary>Validação real contra SOMA_DESENV (B2.9) — autorizada exclusivamente para esta sprint neste
/// banco de desenvolvimento. Usa CNPJs sintéticos claramente identificáveis (prefixo fixo <c>91999000000xx</c>)
/// e sempre tenta limpar os registros criados, nunca reaproveitando CNPJ de fornecedor real.
///
/// IMPORTANTE (achado da rodada de diagnóstico de 12/08/2026): a limpeza NUNCA deve mascarar uma falha de
/// asserção — um `finally` que relança sua própria exceção sobre uma exceção/assert já em voo apaga a
/// evidência original. Por isso a limpeza aqui é sempre isolada em try/catch com log explícito via
/// <see cref="ITestOutputHelper"/>, nunca dentro de um `finally` que possa lançar por cima do resultado real.
/// </summary>
public sealed class GarantirFornecedorNoErpIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public async Task GarantirAsync_Should_Create_Cadastro_And_Fornecedor_For_Unknown_Cnpj()
    {
        var (configuration, connectionString) = LoadConfiguration();
        if (connectionString is null) return;

        var cnpj = SyntheticCnpj(3);
        await EnsureNotPresentAsync(connectionString, cnpj);
        var adapter = new SomaGarantirFornecedorErpAdapter(configuration, NullLogger<SomaGarantirFornecedorErpAdapter>.Instance);

        try
        {
            var resultado = await ExecutarComDiagnosticoAsync(() => adapter.GarantirAsync(
                new GarantirFornecedorErpRequest("DEFAULT", cnpj, "Teste B29 Criacao3", "Teste B29 Criacao3 Razao Social", "São Paulo", "SP", "BRASIL", true, "b29-create-3")));

            Assert.Equal(OperacaoGarantirFornecedorErp.Criado, resultado.Operacao);
            Assert.False(string.IsNullOrWhiteSpace(resultado.IdentificadorExterno));

            await using var connection = await OpenAsync(connectionString);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT c.[INDICA_FORNECEDOR], f.[COD_FORNECEDOR] FROM [dbo].[CADASTRO_CLI_FOR] c LEFT JOIN [dbo].[FORNECEDORES] f ON f.[CLIFOR] = c.[COD_CLIFOR] WHERE c.[CGC_CPF] = @cnpj";
            command.Parameters.AddWithValue("@cnpj", cnpj);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(1, Convert.ToInt32(reader["INDICA_FORNECEDOR"]));
            Assert.False(reader["COD_FORNECEDOR"] is DBNull);
        }
        finally
        {
            await CleanupSafeAsync(connectionString, cnpj);
        }
    }

    [Fact]
    public async Task GarantirAsync_Should_Converge_To_Update_When_Fornecedor_Already_Exists()
    {
        var (configuration, connectionString) = LoadConfiguration();
        if (connectionString is null) return;

        var cnpj = SyntheticCnpj(4);
        await EnsureNotPresentAsync(connectionString, cnpj);
        var adapter = new SomaGarantirFornecedorErpAdapter(configuration, NullLogger<SomaGarantirFornecedorErpAdapter>.Instance);

        try
        {
            var primeira = await ExecutarComDiagnosticoAsync(() => adapter.GarantirAsync(
                new GarantirFornecedorErpRequest("DEFAULT", cnpj, "Teste B29 Converg4", "Teste B29 Converg4 Razao", "Extrema", "MG", "BRASIL", true, "b29-converge-4-1")));
            Assert.Equal(OperacaoGarantirFornecedorErp.Criado, primeira.Operacao);

            var segunda = await ExecutarComDiagnosticoAsync(() => adapter.GarantirAsync(
                new GarantirFornecedorErpRequest("DEFAULT", cnpj, "Teste B29 Converg4", "Teste B29 Converg4 Razao", "Extrema", "MG", "BRASIL", true, "b29-converge-4-2")));

            Assert.Equal(OperacaoGarantirFornecedorErp.Atualizado, segunda.Operacao);
            Assert.Equal(primeira.IdentificadorExterno, segunda.IdentificadorExterno);
        }
        finally
        {
            await CleanupSafeAsync(connectionString, cnpj);
        }
    }

    /// <summary>Retest do Gate de Fornecedores (2026-09-01), item 8 — prova real contra SOMA_DESENV de que o
    /// "falso sucesso" relatado (badge "Sincronizado" sem ENDERECO/NUMERO/BAIRRO/COMPLEMENTO gravados) foi
    /// eliminado: cria um fornecedor sintético, depois chama GarantirAsync de novo com um endereço diferente
    /// (simulando a edição feita em "Enviar ao ERP") e confirma, lendo diretamente CADASTRO_CLI_FOR, que os
    /// valores realmente mudaram no Linx — nunca aceitando o retorno do Adapter como prova.</summary>
    [Fact]
    public async Task GarantirAsync_Should_Persist_Address_Fields_On_Create_And_On_Update()
    {
        var (configuration, connectionString) = LoadConfiguration();
        if (connectionString is null) return;

        var cnpj = SyntheticCnpj(6);
        await EnsureNotPresentAsync(connectionString, cnpj);
        var adapter = new SomaGarantirFornecedorErpAdapter(configuration, NullLogger<SomaGarantirFornecedorErpAdapter>.Instance);

        try
        {
            var criado = await ExecutarComDiagnosticoAsync(() => adapter.GarantirAsync(new GarantirFornecedorErpRequest(
                "DEFAULT", cnpj, "Teste B29 Endereco6", "Teste B29 Endereco6 Razao", "São Paulo", "SP", "BRASIL", true, "b29-endereco-6-create",
                Cep: "01310-100", Logradouro: "Avenida Paulista", Numero: "1000", Complemento: "Sala A", Bairro: "Bela Vista")));
            Assert.Equal(OperacaoGarantirFornecedorErp.Criado, criado.Operacao);
            await AssertEnderecoGravadoAsync(connectionString, criado.IdentificadorExterno, "São Paulo", "Avenida Paulista", "1000", "Sala A", "Bela Vista", "01310-100");

            var atualizado = await ExecutarComDiagnosticoAsync(() => adapter.GarantirAsync(new GarantirFornecedorErpRequest(
                "DEFAULT", cnpj, "Teste B29 Endereco6", "Teste B29 Endereco6 Razao", "Rio de Janeiro", "RJ", "BRASIL", true, "b29-endereco-6-update",
                Cep: "22041-001", Logradouro: "Avenida Atlantica", Numero: "2000", Complemento: "Sala B", Bairro: "Copacabana")));
            Assert.Equal(OperacaoGarantirFornecedorErp.Atualizado, atualizado.Operacao);
            await AssertEnderecoGravadoAsync(connectionString, atualizado.IdentificadorExterno, "Rio de Janeiro", "Avenida Atlantica", "2000", "Sala B", "Copacabana", "22041-001");
        }
        finally
        {
            await CleanupSafeAsync(connectionString, cnpj);
        }
    }

    private static async Task AssertEnderecoGravadoAsync(string connectionString, string codClifor, string cidade, string logradouro, string numero, string complemento, string bairro, string cep)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT [CIDADE], [ENDERECO], [NUMERO], [COMPLEMENTO], [BAIRRO], [CEP] FROM [dbo].[CADASTRO_CLI_FOR] WHERE [COD_CLIFOR] = @id";
        command.Parameters.AddWithValue("@id", codClifor);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(cidade, Convert.ToString(reader["CIDADE"])?.Trim());
        Assert.Equal(logradouro, Convert.ToString(reader["ENDERECO"])?.Trim());
        Assert.Equal(numero, Convert.ToString(reader["NUMERO"])?.Trim());
        Assert.Equal(complemento, Convert.ToString(reader["COMPLEMENTO"])?.Trim());
        Assert.Equal(bairro, Convert.ToString(reader["BAIRRO"])?.Trim());
        Assert.Equal(cep, Convert.ToString(reader["CEP"])?.Trim());
    }

    [Fact]
    public async Task GarantirAsync_Should_Rollback_Completely_When_Cadastro_Insert_Collides()
    {
        // Prova de atomicidade (item 9 do diagnóstico) com uma falha real e determinística: pré-cria em
        // CADASTRO_CLI_FOR um registro "decoy" cujo NOME_CLIFOR é exatamente o que o Adapter vai gerar
        // (mesmo texto sanitizado) para um CNPJ diferente. O primeiro INSERT do Adapter (CADASTRO_CLI_FOR)
        // colide com a PK (NOME_CLIFOR) do decoy e falha de verdade contra o SQL Server real — não é uma
        // falha simulada/mockada. Nota de engenharia: dado que FORNECEDORES.FORNECEDOR tem FK física para
        // CADASTRO_CLI_FOR.NOME_CLIFOR (achado desta rodada), e o próprio Adapter gera o mesmo NOME_CLIFOR
        // fresco para os dois INSERTs de um CREATE, não existe um cenário real de "primeiro insert sucede,
        // segundo insert (FORNECEDORES) falha" para o fluxo CREATE — os dois estão sempre alinhados por
        // construção. A falha real e isolável do segundo INSERT só existe no fluxo ADD_ROLE (coberto pelo
        // teste seguinte), motivo pelo qual o bug real desta rodada (uso do NOME_CLIFOR errado) só afetava
        // ADD_ROLE, nunca CREATE.
        var (configuration, connectionString) = LoadConfiguration();
        if (connectionString is null) return;

        var cnpj = SyntheticCnpj(5);
        await EnsureNotPresentAsync(connectionString, cnpj);
        const string nomeDecoy = "TESTE B29 ROLLBACK5";
        await PreCriarCadastroDecoyAsync(connectionString, "777775", nomeDecoy, cnpjDecoy: "00000000000000");

        var adapter = new SomaGarantirFornecedorErpAdapter(configuration, NullLogger<SomaGarantirFornecedorErpAdapter>.Instance);
        try
        {
            var ex = await Assert.ThrowsAsync<ErpFornecedorEscritaException>(() => adapter.GarantirAsync(
                new GarantirFornecedorErpRequest("DEFAULT", cnpj, "Teste B29 Rollback5", "Teste B29 Rollback5 Razao", "São Paulo", "SP", "BRASIL", true, "b29-rollback-5")));
            output.WriteLine($"Exceção esperada capturada: {ex.Tipo} — {ex.Message}");
            if (ex.InnerException is SqlException sql)
                output.WriteLine($"SqlException real: Number={sql.Number}, State={sql.State}, Class={sql.Class}, Procedure={sql.Procedure}, LineNumber={sql.LineNumber}, Message={sql.Message}");

            await using var connection = await OpenAsync(connectionString);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM [dbo].[CADASTRO_CLI_FOR] WHERE [CGC_CPF] = @cnpj";
            command.Parameters.AddWithValue("@cnpj", cnpj);
            var restantes = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(0, restantes); // ROLLBACK TOTAL: nenhum registro para o NOSSO cnpj deve sobrar após a falha.
        }
        finally
        {
            await CleanupSafeAsync(connectionString, cnpj);
            await CleanupDecoyAsync(connectionString, "777775");
        }
    }

    [Fact]
    public async Task GarantirAsync_Should_Add_Fornecedor_Role_Preserving_Existing_Client_And_Name()
    {
        // Cenário B do brief (CADASTRO_CLI_FOR existente como Cliente, sem Fornecedor) + prova direta da
        // correção do bug real encontrado nesta rodada: FORNECEDORES.FORNECEDOR tem FK física para
        // CADASTRO_CLI_FOR.NOME_CLIFOR (XFK12594_FORNECEDORES). O nome informado nesta chamada é
        // deliberadamente DIFERENTE do NOME_CLIFOR já gravado — antes da correção, o Adapter recalculava o
        // nome a partir do request e o INSERT em FORNECEDORES violava a FK (ADD_ROLE falhava sempre que o
        // nome da chamada não fosse byte-a-byte igual ao já cadastrado). Após a correção, o Adapter reusa o
        // NOME_CLIFOR existente — a operação deve suceder, preservar o papel Cliente e o nome original.
        var (configuration, connectionString) = LoadConfiguration();
        if (connectionString is null) return;

        var cnpj = SyntheticCnpj(6);
        await EnsureNotPresentAsync(connectionString, cnpj);
        const string codClifor = "777776";
        const string nomeOriginal = "TESTE B29 ADDROLE ORIG"; // <=25 chars: NOME_CLIFOR e FORNECEDOR sao varchar(25)
        await PreCriarCadastroDecoyAsync(connectionString, codClifor, nomeOriginal, cnpj, indicaCliente: true);

        var adapter = new SomaGarantirFornecedorErpAdapter(configuration, NullLogger<SomaGarantirFornecedorErpAdapter>.Instance);
        try
        {
            var resultado = await ExecutarComDiagnosticoAsync(() => adapter.GarantirAsync(
                new GarantirFornecedorErpRequest("DEFAULT", cnpj, "Teste B29 AddRole Nome Diferente", "Teste B29 AddRole Nome Diferente Razao", "São Paulo", "SP", "BRASIL", true, "b29-addrole-6")));

            Assert.Equal(OperacaoGarantirFornecedorErp.PapelAdicionado, resultado.Operacao);
            Assert.Equal(codClifor, resultado.IdentificadorExterno);

            await using var connection = await OpenAsync(connectionString);
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT c.[NOME_CLIFOR], c.[INDICA_CLIENTE], c.[INDICA_FORNECEDOR], f.[FORNECEDOR] " +
                "FROM [dbo].[CADASTRO_CLI_FOR] c LEFT JOIN [dbo].[FORNECEDORES] f ON f.[CLIFOR] = c.[COD_CLIFOR] " +
                "WHERE c.[COD_CLIFOR] = @id";
            command.Parameters.AddWithValue("@id", codClifor);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(nomeOriginal, Convert.ToString(reader["NOME_CLIFOR"])?.Trim());
            Assert.Equal(1, Convert.ToInt32(reader["INDICA_CLIENTE"])); // papel Cliente preservado
            Assert.Equal(1, Convert.ToInt32(reader["INDICA_FORNECEDOR"])); // papel Fornecedor adicionado
            Assert.Equal(nomeOriginal, Convert.ToString(reader["FORNECEDOR"])?.Trim()); // FK satisfeita com o nome ORIGINAL, não o da chamada
        }
        finally
        {
            await CleanupSafeAsync(connectionString, cnpj);
        }
    }

    [Fact]
    public async Task GarantirAsync_Concurrent_Create_For_Same_Cnpj_Should_Result_In_Single_Fornecedor()
    {
        // Item 20 do diagnóstico: duas chamadas concorrentes de CREATE para o MESMO CNPJ. A proteção do
        // Adapter é o `WITH (UPDLOCK, HOLDLOCK)` na reconsulta (dentro da transação) — sem índice único
        // físico em CADASTRO_CLI_FOR.CGC_CPF (confirmado nesta rodada: não existe), a garantia é
        // aplicacional/de lock, não uma constraint física. Este teste prova o comportamento observado nesta
        // instância de SOMA_DESENV com duas transações reais disparadas ao mesmo tempo.
        var (configuration, connectionString) = LoadConfiguration();
        if (connectionString is null) return;

        var cnpj = SyntheticCnpj(7);
        await EnsureNotPresentAsync(connectionString, cnpj);
        var adapterA = new SomaGarantirFornecedorErpAdapter(configuration, NullLogger<SomaGarantirFornecedorErpAdapter>.Instance);
        var adapterB = new SomaGarantirFornecedorErpAdapter(configuration, NullLogger<SomaGarantirFornecedorErpAdapter>.Instance);

        try
        {
            var requestA = new GarantirFornecedorErpRequest("DEFAULT", cnpj, "Teste B29 Concorrencia7", "Teste B29 Concorrencia7 Razao", "São Paulo", "SP", "BRASIL", true, "b29-concorrencia-7-a");
            var requestB = requestA with { CorrelationId = "b29-concorrencia-7-b" };

            var tarefaA = adapterA.GarantirAsync(requestA);
            var tarefaB = adapterB.GarantirAsync(requestB);
            var resultados = await Task.WhenAll(WrapAsResult(tarefaA), WrapAsResult(tarefaB));

            foreach (var (resultado, erro) in resultados)
            {
                if (erro is not null)
                    output.WriteLine($"Uma das chamadas concorrentes falhou (esperado ser recuperável ou convergir): {erro.GetType().Name} — {erro.Message}");
            }

            await using var connection = await OpenAsync(connectionString);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM [dbo].[CADASTRO_CLI_FOR] WHERE [CGC_CPF] = @cnpj";
            command.Parameters.AddWithValue("@cnpj", cnpj);
            var totalCadastros = Convert.ToInt32(await command.ExecuteScalarAsync());
            output.WriteLine($"Total de CADASTRO_CLI_FOR para o CNPJ concorrente: {totalCadastros}");
            Assert.Equal(1, totalCadastros); // no máximo um fornecedor final, nunca duplicidade.

            await using var commandF = connection.CreateCommand();
            commandF.CommandText = "SELECT COUNT(1) FROM [dbo].[FORNECEDORES] f JOIN [dbo].[CADASTRO_CLI_FOR] c ON c.[COD_CLIFOR] = f.[CLIFOR] WHERE c.[CGC_CPF] = @cnpj";
            commandF.Parameters.AddWithValue("@cnpj", cnpj);
            var totalFornecedores = Convert.ToInt32(await commandF.ExecuteScalarAsync());
            output.WriteLine($"Total de FORNECEDORES para o CNPJ concorrente: {totalFornecedores}");
            Assert.Equal(1, totalFornecedores);
        }
        finally
        {
            await CleanupSafeAsync(connectionString, cnpj);
        }
    }

    [Fact]
    public async Task GarantirAsync_Should_Truncate_Long_RazaoSocial_Instead_Of_Failing_On_Erp_Column_Length()
    {
        // Regressão B2.9 (validação E2E, 13/08/2026): NOME_CLIFOR e FORNECEDOR são varchar(25) reais no ERP.
        // Razões sociais completas (comuns no Brasil, ex.: "... LTDA" por extenso) frequentemente excedem 25
        // caracteres após sanitização — sem truncamento, o INSERT falhava com "String or binary data would be
        // truncated" e o fornecedor ficava permanentemente Pendente (falha determinística, nenhum retry a
        // resolvia). O nome usado aqui tem mais de 25 caracteres já sanitizado (apenas letras/dígitos/espaço).
        var (configuration, connectionString) = LoadConfiguration();
        if (connectionString is null) return;

        var cnpj = SyntheticCnpj(8);
        await EnsureNotPresentAsync(connectionString, cnpj);
        const string nomeLongo = "Teste B29 Nome Muito Longo Truncamento8";
        var adapter = new SomaGarantirFornecedorErpAdapter(configuration, NullLogger<SomaGarantirFornecedorErpAdapter>.Instance);

        try
        {
            var resultado = await ExecutarComDiagnosticoAsync(() => adapter.GarantirAsync(
                new GarantirFornecedorErpRequest("DEFAULT", cnpj, nomeLongo, nomeLongo, "São Paulo", "SP", "BRASIL", true, "b29-truncamento-8")));

            Assert.Equal(OperacaoGarantirFornecedorErp.Criado, resultado.Operacao);

            await using var connection = await OpenAsync(connectionString);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT [NOME_CLIFOR] FROM [dbo].[CADASTRO_CLI_FOR] WHERE [CGC_CPF] = @cnpj";
            command.Parameters.AddWithValue("@cnpj", cnpj);
            var nomePersistido = Convert.ToString(await command.ExecuteScalarAsync())?.Trim();
            Assert.NotNull(nomePersistido);
            Assert.True(nomePersistido!.Length <= 25, $"NOME_CLIFOR persistido excedeu 25 caracteres: '{nomePersistido}' ({nomePersistido.Length}).");
        }
        finally
        {
            await CleanupSafeAsync(connectionString, cnpj);
        }
    }

    private static async Task<(GarantirFornecedorErpResultado? Resultado, Exception? Erro)> WrapAsResult(Task<GarantirFornecedorErpResultado> task)
    {
        try { return (await task, null); }
        catch (Exception ex) { return (null, ex); }
    }

    private async Task<T> ExecutarComDiagnosticoAsync<T>(Func<Task<T>> acao)
    {
        try
        {
            return await acao();
        }
        catch (ErpFornecedorEscritaException ex)
        {
            output.WriteLine($"ErpFornecedorEscritaException: Tipo={ex.Tipo}, Mensagem={ex.Message}");
            if (ex.InnerException is SqlException sql)
                output.WriteLine($"SqlException real: Number={sql.Number}, State={sql.State}, Class={sql.Class}, Procedure={sql.Procedure}, LineNumber={sql.LineNumber}, Message={sql.Message}");
            throw;
        }
    }

    /// <summary>Além do padrão de skip silencioso das demais integrações reais (connection string ausente/placeholder),
    /// estes testes exigem o opt-in explícito <c>B29_REAL_WRITE_TESTS=1</c>. Nunca habilitar em CI.</summary>
    private static (IConfiguration Configuration, string? ConnectionString) LoadConfiguration()
    {
        if (Environment.GetEnvironmentVariable("B29_REAL_WRITE_TESTS") != "1") return (null!, null);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("ErpConnection");
        return string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal)
            ? (configuration, null)
            : (configuration, connectionString);
    }

    private static string SyntheticCnpj(int indice)
    {
        var baseDigits = $"9199900000{indice:00}";
        var dv = DigitoVerificadorCnpj(baseDigits);
        return baseDigits + dv;
    }

    private static string DigitoVerificadorCnpj(string base12)
    {
        int[] pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] pesos2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var d1 = CalcularDigito(base12, pesos1);
        var d2 = CalcularDigito(base12 + d1, pesos2);
        return $"{d1}{d2}";
    }

    private static int CalcularDigito(string numero, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < pesos.Length; i++) soma += (numero[i] - '0') * pesos[i];
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static async Task EnsureNotPresentAsync(string connectionString, string cnpj)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM [dbo].[CADASTRO_CLI_FOR] WHERE [CGC_CPF] = @cnpj";
        command.Parameters.AddWithValue("@cnpj", cnpj);
        var existentes = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (existentes > 0)
        {
            throw new InvalidOperationException(
                $"CNPJ sintético de teste {cnpj} já existe em SOMA_DESENV — abortando para não reaproveitar cadastro real. " +
                "Escolha outro índice sintético ou limpe manualmente o registro de teste remanescente.");
        }
    }

    /// <summary>Insere diretamente (fora do Adapter) um registro "decoy" em CADASTRO_CLI_FOR, com um
    /// COD_CLIFOR/CLIFOR fixo e reservado a estes testes (nunca usar um valor que possa colidir com o
    /// mecanismo sequencial real — por isso um valor alto e verificado como livre antes do uso). Usado tanto
    /// para o teste de colisão de atomicidade (sem papel Fornecedor, CNPJ decoy inerte) quanto para o teste
    /// real de ADD_ROLE (papel Cliente pré-existente, CNPJ do próprio teste).</summary>
    private static async Task PreCriarCadastroDecoyAsync(string connectionString, string codClifor, string nomeClifor, string cnpjDecoy, bool indicaCliente = false)
    {
        await using var connection = await OpenAsync(connectionString);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO [dbo].[CADASTRO_CLI_FOR] ([NOME_CLIFOR], [CLIFOR], [COD_CLIFOR], [CGC_CPF], [RAZAO_SOCIAL], [RG_IE], [UF], [COBRANCA_UF], [ENTREGA_UF], [COBRANCA_CGC], [CADASTRAMENTO], [COBRANCA_IE], [ENTREGA_CGC], [ENTREGA_IE], [PAIS], [COBRANCA_PAIS], [ENTREGA_PAIS], [INDICA_FORNECEDOR], [INDICA_CLIENTE]) " +
            "VALUES (@nome, @id, @id, @cnpj, @nome, '', 'SP', 'SP', 'SP', @cnpj, GETDATE(), '', @cnpj, '', 'BRASIL', 'BRASIL', 'BRASIL', 0, @indicaCliente)";
        command.Parameters.AddWithValue("@nome", nomeClifor);
        command.Parameters.AddWithValue("@id", codClifor);
        command.Parameters.AddWithValue("@cnpj", cnpjDecoy);
        command.Parameters.AddWithValue("@indicaCliente", indicaCliente ? 1 : 0);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Remove o decoy criado por <see cref="PreCriarCadastroDecoyAsync"/> — mesma ressalva de
    /// robustez do <see cref="CleanupSafeAsync"/> (DELETE em CADASTRO_CLI_FOR é lento pela cascata de
    /// triggers, nunca falhar silenciosamente sem aviso).</summary>
    private async Task CleanupDecoyAsync(string connectionString, string codClifor)
    {
        try
        {
            await using var connection = await OpenAsync(connectionString);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            await using (var fornecedores = connection.CreateCommand())
            {
                fornecedores.Transaction = transaction; fornecedores.CommandTimeout = 200;
                fornecedores.CommandText = "DELETE FROM [dbo].[FORNECEDORES] WHERE [CLIFOR] = @id";
                fornecedores.Parameters.AddWithValue("@id", codClifor);
                await fornecedores.ExecuteNonQueryAsync();
            }
            await using (var cadastro = connection.CreateCommand())
            {
                cadastro.Transaction = transaction; cadastro.CommandTimeout = 200;
                cadastro.CommandText = "DELETE FROM [dbo].[CADASTRO_CLI_FOR] WHERE [COD_CLIFOR] = @id";
                cadastro.Parameters.AddWithValue("@id", codClifor);
                await cadastro.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            output.WriteLine($"AVISO: limpeza do decoy {codClifor} falhou ({ex.GetType().Name}: {ex.Message}). Requer limpeza manual/DBA.");
        }
    }

    /// <summary>Nunca lança — uma falha de limpeza NUNCA deve mascarar o resultado real do teste (achado
    /// desta rodada de diagnóstico: um `finally` que relança sobrepunha a evidência da asserção original).
    ///
    /// ACHADO CRÍTICO desta rodada (12/08/2026): as duas instruções DELETE aqui rodavam em um único
    /// <see cref="SqlCommand"/> SEM transação explícita — cada DELETE de um batch multi-statement sem
    /// `BEGIN TRAN` faz seu próprio autocommit individual no SQL Server. Como `DELETE FROM CADASTRO_CLI_FOR`
    /// dispara uma cascata de ~11 triggers (auditoria, ETL/WETL) e por isso é genuinamente lento (~2-3 minutos
    /// nesta instância de SOMA_DESENV — não é bloqueio de outra sessão, é o próprio custo da cascata), o
    /// primeiro DELETE (FORNECEDORES, rápido) commitava sozinho e só o segundo (CADASTRO_CLI_FOR) estourava
    /// o timeout do cliente — deixando exatamente o tipo de órfão relatado na rodada anterior da B2.9
    /// (CADASTRO_CLI_FOR com INDICA_FORNECEDOR=1 sem FORNECEDORES correspondente). Este NÃO era um bug do
    /// Adapter: era um bug desta própria limpeza de teste. Corrigido envolvendo os dois DELETEs em uma
    /// transação explícita (tudo ou nada) e usando um timeout generoso (o real, medido, gira em torno de
    /// 2-3 minutos por causa da cascata de triggers).</summary>
    private async Task CleanupSafeAsync(string? connectionString, string cnpj)
    {
        if (connectionString is null) return;
        SqlConnection? connection = null;
        SqlTransaction? transaction = null;
        try
        {
            connection = await OpenAsync(connectionString);
            transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            await using (var fornecedores = connection.CreateCommand())
            {
                fornecedores.Transaction = transaction; fornecedores.CommandTimeout = 200;
                fornecedores.CommandText = "DELETE FROM [dbo].[FORNECEDORES] WHERE [CLIFOR] IN (SELECT [COD_CLIFOR] FROM [dbo].[CADASTRO_CLI_FOR] WHERE [CGC_CPF] = @cnpj)";
                fornecedores.Parameters.AddWithValue("@cnpj", cnpj);
                await fornecedores.ExecuteNonQueryAsync();
            }

            await using (var cadastro = connection.CreateCommand())
            {
                cadastro.Transaction = transaction; cadastro.CommandTimeout = 200; // cascata de triggers: ~2-3min medidos nesta instância
                cadastro.CommandText = "DELETE FROM [dbo].[CADASTRO_CLI_FOR] WHERE [CGC_CPF] = @cnpj";
                cadastro.Parameters.AddWithValue("@cnpj", cnpj);
                await cadastro.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            output.WriteLine($"AVISO: limpeza automática do CNPJ sintético {cnpj} falhou ({ex.GetType().Name}: {ex.Message}). " +
                              "Registro pode ter ficado órfão em SOMA_DESENV — requer limpeza manual/DBA. " +
                              "Como a limpeza agora é transacional, NÃO deve deixar órfão parcial (ou os dois DELETEs valem, ou nenhum).");
            if (transaction is not null) { try { await transaction.RollbackAsync(); } catch { /* melhor esforço */ } }
        }
        finally
        {
            if (connection is not null) await connection.DisposeAsync();
        }
    }

    private static async Task<SqlConnection> OpenAsync(string connectionString)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
