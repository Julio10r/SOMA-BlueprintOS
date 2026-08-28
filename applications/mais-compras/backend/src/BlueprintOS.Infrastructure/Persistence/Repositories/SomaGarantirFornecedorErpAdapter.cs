using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>Adapter Linx de Fornecedor/CNPJ (B2.9, ADR-0023). Isolado do SOMA_DESENV — nenhum vocabulário
/// físico do Linx (CLIFOR, CADASTRO_CLI_FOR, LX_SEQUENCIAL) atravessa a fronteira de <see cref="IGarantirFornecedorErpAdapter"/>.
///
/// Semântica: "garantir/atualizar fornecedor no ERP", nunca "inserir". A cada chamada:
///  1. reconsulta o estado real do ERP por CNPJ imediatamente antes de decidir a operação (nunca confia em
///     estado anterior/cache/frontend) — mitiga TOCTOU (Gate Pré-B2.9, seção 7-A);
///  2. decide CREATE / ADD_ROLE / UPDATE a partir do estado observado;
///  3. executa a operação física em uma única transação curta, sem chamada externa dentro dela;
///  4. nunca cria FILIAIS/CLIENTES_ATACADO, nunca destrói papéis existentes de CADASTRO_CLI_FOR.
///
/// EMPRESA do Linx é fixada em 1 (decisão do Product Owner — a SOMA não usa a separação de
/// empresa/grupo econômico) e nunca é exposta acima desta classe.
/// </summary>
public sealed class SomaGarantirFornecedorErpAdapter(IConfiguration configuration, ILogger<SomaGarantirFornecedorErpAdapter> logger)
    : IGarantirFornecedorErpAdapter, IGovernedToolAdapter, ISnapshotCapableAdapter
{
    private const int Empresa = 1;

    public string ErpSistema => "SOMA_DESENV";

    // --- Governed write stack surface -------------------------------------------------------------------
    // Added additively in the Production Write Verification & Recovery work. NONE of the SQL, transaction,
    // UPDLOCK/HOLDLOCK, commit/rollback or error-classification logic below was changed: GarantirAsync is
    // byte-for-byte the same flow it was. What is new is (a) declaring this adapter to the Tool Gateway and
    // (b) exposing a READ-ONLY snapshot capture so a recovery package can hold a real before/after state.

    public const string CapabilityId = "soma-fornecedor-governed-write";
    public const string OwnerAgentId = "linx-database-specialist-agent";

    public string Capability => CapabilityId;

    public string OwnerAgent => OwnerAgentId;

    /// <summary>Development only. This adapter resolves the DEVELOPMENT connection profile and nothing else;
    /// it can never be pointed at Production by a request.</summary>
    public IReadOnlyList<string> AllowedConnectionProfiles => [WriteVerificationProfileSeeds.LinxDevelopment];

    public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(new SomaLinxDryRunPreview(
            request.Proposal.System,
            request.Proposal.Environment,
            request.Proposal.Resource,
            request.Proposal.Operation,
            request.Proposal.Fields,
            request.Proposal.FilterSummary,
            request.Proposal.ExpectedAffectedRows,
            request.Proposal.Purpose,
            request.ConnectionProfile,
            request.PolicyDecision.RiskClassification,
            request.PolicyDecision.Status,
            request.ApprovalGrant is null ? "none" : "granted",
            request.Proposal.Reversibility,
            request.ExecutionMode,
            CredentialResolutionRequired: true,
            IdentityPermissionCheckRequired: true,
            SqlGenerated: false,
            ExternalExecutionPerformed: false));
    }

    /// <summary>
    /// READ-ONLY capture of the current state of the records a write is about to touch, keyed by
    /// <c>CGC_CPF=&lt;digits&gt;</c> business keys. No transaction, no lock, no mutation — this is the
    /// before/after photograph a recovery package and a post-write validation are built on.
    /// </summary>
    public async Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(businessKeys);
        var cnpjs = businessKeys.Select(ExtrairCnpjDaChaveDeNegocio).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToList();
        if (cnpjs.Count == 0) return [];

        var cadastro = new List<IReadOnlyDictionary<string, string?>>();
        var fornecedores = new List<IReadOnlyDictionary<string, string?>>();

        await using var connection = await OpenAsync(cancellationToken);
        foreach (var cnpj in cnpjs)
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = TimeoutSeconds;
            command.CommandText =
                "SELECT c.[COD_CLIFOR], c.[NOME_CLIFOR], c.[CGC_CPF], c.[INDICA_FORNECEDOR], " +
                "f.[COD_FORNECEDOR], f.[FORNECEDOR], f.[CGC_CPF] AS FORNECEDOR_CGC_CPF, f.[INATIVO] " +
                "FROM [dbo].[CADASTRO_CLI_FOR] c " +
                "LEFT JOIN [dbo].[FORNECEDORES] f ON f.[CLIFOR] = c.[COD_CLIFOR] " +
                "WHERE c.[CGC_CPF] = @cnpj";
            command.Parameters.Add(new SqlParameter("@cnpj", cnpj));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                cadastro.Add(new Dictionary<string, string?>
                {
                    ["COD_CLIFOR"] = Texto(reader["COD_CLIFOR"]),
                    ["NOME_CLIFOR"] = Texto(reader["NOME_CLIFOR"]),
                    ["CGC_CPF"] = Texto(reader["CGC_CPF"]),
                    ["INDICA_FORNECEDOR"] = Texto(reader["INDICA_FORNECEDOR"]),
                });

                if (reader["COD_FORNECEDOR"] is not null and not DBNull)
                {
                    fornecedores.Add(new Dictionary<string, string?>
                    {
                        ["COD_FORNECEDOR"] = Texto(reader["COD_FORNECEDOR"]),
                        ["FORNECEDOR"] = Texto(reader["FORNECEDOR"]),
                        ["CGC_CPF"] = Texto(reader["FORNECEDOR_CGC_CPF"]),
                        ["INATIVO"] = Texto(reader["INATIVO"]),
                    });
                }
            }
        }

        return
        [
            new RecoveryDataSet("CADASTRO_CLI_FOR", cadastro),
            new RecoveryDataSet("FORNECEDORES", fornecedores),
        ];
    }

    /// <summary>Accepts <c>CGC_CPF=00000000000191</c> or a bare document, and keeps only digits.</summary>
    internal static string ExtrairCnpjDaChaveDeNegocio(string businessKey)
    {
        if (string.IsNullOrWhiteSpace(businessKey)) return string.Empty;
        var separador = businessKey.IndexOf('=');
        var valor = separador >= 0 ? businessKey[(separador + 1)..] : businessKey;
        return SomenteDigitos(valor);
    }

    /// <summary>Stringifies a column value for a recovery snapshot. A SQL <c>bit</c> column arrives as a CLR
    /// <see cref="bool"/> and <see cref="Convert.ToString(object)"/> would render it "True"/"False" —
    /// inconsistent with the "0"/"1" convention every expected-after payload in this codebase already uses for
    /// bit columns (see <c>GovernedGarantirFornecedorService</c>). Normalizing here, once, is what makes
    /// post-write validation's string comparison actually match a real bit column instead of failing on a
    /// harmless formatting difference.</summary>
    private static string? Texto(object? value) => value switch
    {
        null or DBNull => null,
        bool boolValue => boolValue ? "1" : "0",
        _ => Convert.ToString(value)?.Trim(),
    };

    public async Task<GarantirFornecedorErpResultado> GarantirAsync(GarantirFornecedorErpRequest request, CancellationToken cancellationToken = default)
    {
        var cnpj = SomenteDigitos(request.DocumentoFiscal);
        if (string.IsNullOrWhiteSpace(cnpj)) throw new ErpFornecedorEscritaException(ErpFornecedorErro.Validacao, "Documento fiscal é obrigatório para garantir o fornecedor no ERP.");
        if (string.IsNullOrWhiteSpace(request.Nome)) throw new ErpFornecedorEscritaException(ErpFornecedorErro.Validacao, "Nome é obrigatório para garantir o fornecedor no ERP.");

        var nomeClifor = SanitizarNomeClifor(request.Nome);
        if (string.IsNullOrWhiteSpace(nomeClifor)) throw new ErpFornecedorEscritaException(ErpFornecedorErro.Validacao, "Nome do fornecedor resultou vazio após sanitização.");

        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var sqlTransaction = (SqlTransaction)transaction;
        try
        {
            var estadoAtual = await ReconsultarComLockAsync(connection, sqlTransaction, cnpj, cancellationToken);

            OperacaoGarantirFornecedorErp operacao;
            string codigoExterno;

            if (estadoAtual is null)
            {
                codigoExterno = await ObterProximoCliforAsync(connection, sqlTransaction, cancellationToken);
                await CriarCadastroEFornecedorAsync(connection, sqlTransaction, codigoExterno, nomeClifor, request, cnpj, cancellationToken);
                operacao = OperacaoGarantirFornecedorErp.Criado;
            }
            else if (!estadoAtual.Value.PossuiPapelFornecedor)
            {
                codigoExterno = estadoAtual.Value.CodClifor;
                await AdicionarPapelFornecedorAsync(connection, sqlTransaction, codigoExterno, estadoAtual.Value.NomeClifor, request, cnpj, cancellationToken);
                operacao = OperacaoGarantirFornecedorErp.PapelAdicionado;
            }
            else
            {
                codigoExterno = estadoAtual.Value.CodClifor;
                await AtualizarFornecedorAsync(connection, sqlTransaction, codigoExterno, request, cnpj, cancellationToken);
                operacao = OperacaoGarantirFornecedorErp.Atualizado;
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Fornecedor garantido no ERP. ERP {ErpSistema}, BU {BusinessUnit}, Operação {Operacao}, CorrelationId {CorrelationId}",
                ErpSistema, request.BusinessUnit, operacao, request.CorrelationId);

            return new GarantirFornecedorErpResultado(operacao, codigoExterno, request.BusinessUnit, ErpSistema, DateTimeOffset.UtcNow, request.CorrelationId);
        }
        catch (ErpFornecedorEscritaException)
        {
            await RollbackSeguroAsync(transaction);
            throw;
        }
        catch (Exception ex)
        {
            await RollbackSeguroAsync(transaction);
            logger.LogError(ex, "Falha ao garantir fornecedor no ERP. ERP {ErpSistema}, BU {BusinessUnit}, CorrelationId {CorrelationId}", ErpSistema, request.BusinessUnit, request.CorrelationId);
            throw Classificar(ex);
        }
    }

    private async Task RollbackSeguroAsync(System.Data.Common.DbTransaction transaction)
    {
        try { await transaction.RollbackAsync(CancellationToken.None); }
        catch (Exception rollbackError) { logger.LogWarning(rollbackError, "Falha ao desfazer transação ERP ao garantir fornecedor. ERP {ErpSistema}", ErpSistema); }
    }

    private readonly record struct EstadoCadastroErp(string CodClifor, string NomeClifor, bool PossuiPapelFornecedor);

    /// <summary>Reconsulta obrigatória por CNPJ dentro da transação, com <c>UPDLOCK, HOLDLOCK</c> — satisfaz
    /// a exigência de reconsulta imediatamente antes da decisão (seção 24 do brief) e reduz (não elimina —
    /// risco residual documentado para validação em homologação, seção 26/48) a janela de corrida CREATE/CREATE
    /// entre duas operações concorrentes para o mesmo CNPJ.
    ///
    /// Também recupera <c>NOME_CLIFOR</c> — descoberto nesta rodada de diagnóstico (12/08/2026) que
    /// <c>FORNECEDORES.FORNECEDOR</c> possui FK física (<c>XFK12594_FORNECEDORES</c>) para
    /// <c>CADASTRO_CLI_FOR.NOME_CLIFOR</c>: ao adicionar o papel Fornecedor a um cadastro já existente
    /// (ADD_ROLE), o INSERT em FORNECEDORES deve usar o NOME_CLIFOR já gravado, nunca um valor recalculado
    /// a partir do Nome informado nesta chamada — caso contrário o INSERT viola a FK.</summary>
    private async Task<EstadoCadastroErp?> ReconsultarComLockAsync(SqlConnection connection, SqlTransaction transaction, string cnpj, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction; command.CommandTimeout = TimeoutSeconds;
        command.CommandText =
            "SELECT c.[COD_CLIFOR], c.[NOME_CLIFOR], CASE WHEN f.[COD_FORNECEDOR] IS NULL THEN 0 ELSE 1 END AS TemFornecedor " +
            "FROM [dbo].[CADASTRO_CLI_FOR] c WITH (UPDLOCK, HOLDLOCK) " +
            "LEFT JOIN [dbo].[FORNECEDORES] f ON f.[CLIFOR] = c.[COD_CLIFOR] " +
            "WHERE c.[CGC_CPF] = @cnpj";
        command.Parameters.Add(new SqlParameter("@cnpj", cnpj));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var codClifor = Convert.ToString(reader["COD_CLIFOR"])?.Trim() ?? throw new ErpFornecedorEscritaException(ErpFornecedorErro.InconsistenciaEstrutural, "Cadastro do ERP não retornou identificador válido.");
        var nomeClifor = Convert.ToString(reader["NOME_CLIFOR"])?.Trim() ?? throw new ErpFornecedorEscritaException(ErpFornecedorErro.InconsistenciaEstrutural, "Cadastro do ERP não retornou nome válido.");
        var possuiPapel = Convert.ToInt32(reader["TemFornecedor"]) == 1;
        return new EstadoCadastroErp(codClifor, nomeClifor, possuiPapel);
    }

    private async Task<string> ObterProximoCliforAsync(SqlConnection connection, SqlTransaction transaction, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction; command.CommandTimeout = TimeoutSeconds;
        command.CommandText = "DECLARE @CLIFOR CHAR(6); EXEC LX_SEQUENCIAL @TABELA_COLUNA = 'FORNECEDORES.CLIFOR', @EMPRESA = @empresa, @SEQUENCIA = @CLIFOR OUTPUT, @UPDATE_SEQUENCIAL = 1; SELECT @CLIFOR AS CLIFOR;";
        command.Parameters.Add(new SqlParameter("@empresa", Empresa));
        var value = await command.ExecuteScalarAsync(ct);
        var clifor = Convert.ToString(value)?.Trim();
        if (string.IsNullOrWhiteSpace(clifor) || clifor.Length != 6 || clifor.Contains('*'))
            throw new ErpFornecedorEscritaException(ErpFornecedorErro.InconsistenciaEstrutural, "O mecanismo sequencial do ERP retornou um identificador inválido.");
        return clifor;
    }

    private async Task CriarCadastroEFornecedorAsync(SqlConnection connection, SqlTransaction transaction, string codigo, string nomeClifor, GarantirFornecedorErpRequest request, string cnpj, CancellationToken ct)
    {
        await using (var cadastro = connection.CreateCommand())
        {
            cadastro.Transaction = transaction; cadastro.CommandTimeout = TimeoutSeconds;
            cadastro.CommandText =
                "INSERT INTO [dbo].[CADASTRO_CLI_FOR] ([NOME_CLIFOR], [CLIFOR], [COD_CLIFOR], [CGC_CPF], [RAZAO_SOCIAL], [RG_IE], [UF], [COBRANCA_UF], [ENTREGA_UF], [COBRANCA_CGC], [CADASTRAMENTO], [COBRANCA_IE], [ENTREGA_CGC], [ENTREGA_IE], [PAIS], [COBRANCA_PAIS], [ENTREGA_PAIS], [INDICA_FORNECEDOR]) " +
                "VALUES (@nomeClifor, @id, @id, @cnpj, @razaoSocial, '', @uf, @uf, @uf, @cnpj, GETDATE(), '', @cnpj, '', @pais, @pais, @pais, 1)";
            AdicionarParametrosComuns(cadastro, codigo, nomeClifor, request, cnpj);
            var linhas = await cadastro.ExecuteNonQueryAsync(ct);
            // Nota (validado em SOMA_DESENV): CADASTRO_CLI_FOR possui múltiplas triggers ativas (auditoria
            // GSI/GSU/GSD_CADASTRO_CLI_FOR_LOG, filas ETL/WETL) que executam DML adicional na mesma chamada,
            // inflando o rowcount retornado por ExecuteNonQueryAsync além do INSERT em si — por isso o critério
            // de sucesso é "pelo menos 1 linha", nunca "exatamente 1".
            if (linhas < 1) throw new ErpFornecedorEscritaException(ErpFornecedorErro.Persistencia, "O ERP não confirmou a criação do cadastro-base do fornecedor.");
        }

        await using var fornecedor = connection.CreateCommand();
        fornecedor.Transaction = transaction; fornecedor.CommandTimeout = TimeoutSeconds;
        fornecedor.CommandText =
            "INSERT INTO [dbo].[FORNECEDORES] ([COD_FORNECEDOR], [CLIFOR], [FORNECEDOR], [CONDICAO_PGTO], [CGC_CPF], [INATIVO]) " +
            "VALUES (@id, @id, @nomeClifor, '001', @cnpj, @inativo)";
        AdicionarParametrosComuns(fornecedor, codigo, nomeClifor, request, cnpj);
        var linhasFornecedor = await fornecedor.ExecuteNonQueryAsync(ct);
        if (linhasFornecedor < 1) throw new ErpFornecedorEscritaException(ErpFornecedorErro.Persistencia, "O ERP não confirmou a criação do papel de Fornecedor.");
    }

    private async Task AdicionarPapelFornecedorAsync(SqlConnection connection, SqlTransaction transaction, string codigo, string nomeCliforExistente, GarantirFornecedorErpRequest request, string cnpj, CancellationToken ct)
    {
        await using (var cadastro = connection.CreateCommand())
        {
            cadastro.Transaction = transaction; cadastro.CommandTimeout = TimeoutSeconds;
            cadastro.CommandText = "UPDATE [dbo].[CADASTRO_CLI_FOR] SET [INDICA_FORNECEDOR] = 1 WHERE [COD_CLIFOR] = @id";
            cadastro.Parameters.Add(new SqlParameter("@id", codigo));
            await cadastro.ExecuteNonQueryAsync(ct);
        }

        // Usa o NOME_CLIFOR já existente (papel Cliente/Filial preservado) — nunca recalcular a partir do
        // Nome desta chamada: FORNECEDORES.FORNECEDOR tem FK física para CADASTRO_CLI_FOR.NOME_CLIFOR
        // (XFK12594_FORNECEDORES, confirmada em SOMA_DESENV nesta rodada de diagnóstico); usar um valor
        // diferente do já gravado viola a FK e faz o ADD_ROLE falhar inteiramente (rollback).
        await using var fornecedor = connection.CreateCommand();
        fornecedor.Transaction = transaction; fornecedor.CommandTimeout = TimeoutSeconds;
        fornecedor.CommandText =
            "INSERT INTO [dbo].[FORNECEDORES] ([COD_FORNECEDOR], [CLIFOR], [FORNECEDOR], [CONDICAO_PGTO], [CGC_CPF], [INATIVO]) " +
            "VALUES (@id, @id, @nomeClifor, '001', @cnpj, @inativo)";
        AdicionarParametrosComuns(fornecedor, codigo, nomeCliforExistente, request, cnpj);
        var linhas = await fornecedor.ExecuteNonQueryAsync(ct);
        if (linhas < 1) throw new ErpFornecedorEscritaException(ErpFornecedorErro.Persistencia, "O ERP não confirmou a adição do papel de Fornecedor ao cadastro existente.");
    }

    private async Task AtualizarFornecedorAsync(SqlConnection connection, SqlTransaction transaction, string codigo, GarantirFornecedorErpRequest request, string cnpj, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction; command.CommandTimeout = TimeoutSeconds;
        command.CommandText =
            "UPDATE [dbo].[FORNECEDORES] SET [CGC_CPF] = @cnpj, [INATIVO] = @inativo WHERE [COD_FORNECEDOR] = @id; " +
            "UPDATE [dbo].[CADASTRO_CLI_FOR] SET [INDICA_FORNECEDOR] = 1 WHERE [COD_CLIFOR] = @id AND [INDICA_FORNECEDOR] <> 1";
        command.Parameters.Add(new SqlParameter("@id", codigo));
        command.Parameters.Add(new SqlParameter("@cnpj", cnpj));
        command.Parameters.Add(new SqlParameter("@inativo", request.Ativo ? 0 : 1));
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AdicionarParametrosComuns(SqlCommand command, string codigo, string nomeClifor, GarantirFornecedorErpRequest request, string cnpj)
    {
        command.Parameters.Add(new SqlParameter("@id", codigo));
        command.Parameters.Add(new SqlParameter("@nomeClifor", nomeClifor));
        command.Parameters.Add(new SqlParameter("@razaoSocial", string.IsNullOrWhiteSpace(request.RazaoSocial) ? request.Nome.Trim() : request.RazaoSocial.Trim()));
        command.Parameters.Add(new SqlParameter("@cnpj", cnpj));
        command.Parameters.Add(new SqlParameter("@uf", string.IsNullOrWhiteSpace(request.Estado) ? "SP" : request.Estado.Trim()));
        command.Parameters.Add(new SqlParameter("@pais", string.IsNullOrWhiteSpace(request.Pais) ? "BRASIL" : request.Pais.Trim()));
        command.Parameters.Add(new SqlParameter("@inativo", request.Ativo ? 0 : 1));
    }

    /// <summary>Aproxima a sanitização observada em <c>obj_001016G1.prg</c> (Gate Pré-B2.9, seção 6):
    /// maiúsculas, sem espaços nas pontas/duplicados, apenas letras (incl. acentuadas), dígitos e espaço —
    /// evita rejeição pelo trigger <c>LXI_ANM_CADASTRO_CLI_FOR</c>. Truncado em <see cref="NomeCliforMaxLength"/>
    /// (25 — limite físico confirmado de CADASTRO_CLI_FOR.NOME_CLIFOR e FORNECEDORES.FORNECEDOR): sem o
    /// truncamento, qualquer Razão Social sanitizada acima de 25 caracteres (comum no Brasil — ex.: nomes de
    /// LTDA completos) faz o INSERT falhar com "String or binary data would be truncated", deixando o
    /// fornecedor permanentemente em StatusSincronizacao=Pendente (bug real encontrado em validação E2E,
    /// 13/08/2026 — a falha era determinística, nenhuma nova tentativa a resolvia).</summary>
    private const int NomeCliforMaxLength = 25;

    private static string SanitizarNomeClifor(string nome)
    {
        var filtrado = new string(nome.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
        var colapsado = string.Join(' ', filtrado.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var normalizado = colapsado.Trim().ToUpperInvariant();
        return normalizado.Length > NomeCliforMaxLength ? normalizado[..NomeCliforMaxLength].TrimEnd() : normalizado;
    }

    private static string SomenteDigitos(string valor) => new(valor.Where(char.IsDigit).ToArray());

    private static ErpFornecedorEscritaException Classificar(Exception ex) => ex switch
    {
        SqlException { Number: -2 } => new(ErpFornecedorErro.Timeout, "O ERP não respondeu dentro do tempo limite.", ex),
        SqlException sql when EhFalhaDeConectividade(sql) => new(ErpFornecedorErro.Conectividade, "Não foi possível conectar ao ERP.", ex),
        SqlException => new(ErpFornecedorErro.Persistencia, "Falha ao persistir os dados do fornecedor no ERP.", ex),
        TimeoutException => new(ErpFornecedorErro.Timeout, "O ERP não respondeu dentro do tempo limite.", ex),
        _ => new(ErpFornecedorErro.Persistencia, "Falha inesperada ao integrar o fornecedor com o ERP.", ex)
    };

    private static bool EhFalhaDeConectividade(SqlException ex) => ex.Number is 2 or 53 or 233 or 10054 or 10060 or 11001 or 4060 or 18456;

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        string connectionString;
        try
        {
            connectionString = LinxConnectionStringResolver.Resolve(configuration, LinxConnectionProfiles.Development);
        }
        catch (InvalidOperationException ex)
        {
            throw new ErpFornecedorEscritaException(ErpFornecedorErro.Validacao, ex.Message);
        }
        try
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }
        catch (SqlException ex)
        {
            throw Classificar(ex);
        }
    }

    private int TimeoutSeconds => int.TryParse(configuration["ErpIntegration:TimeoutSeconds"], out var value) ? Math.Clamp(value, 1, 120) : 30;
}
