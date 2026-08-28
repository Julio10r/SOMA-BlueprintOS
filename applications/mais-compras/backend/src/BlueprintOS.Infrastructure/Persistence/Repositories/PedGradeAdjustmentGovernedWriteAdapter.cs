#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>
/// Governed write adapter for a PED grade adjustment: updates the grade quantities at positions 1-6
/// (sizes 34,36,38,40,42,44) of one COMPRAS_PRODUTO row, identified by (PEDIDO, PRODUTO, COR_PRODUTO), to
/// match desired absolute values. Reproduces the arithmetic of a prior manual DBA script, but as a
/// parametrized, transactional, single-row governed write — never the script's cursor-based structure, and
/// never touching OP/PROG logic (PRODUCAO_PROG_PROD / PRODUCAO_ORDEM_COR are out of scope).
///
/// Grade position 7+ (CO7/CE7/etc., size 32 and beyond) is NEVER referenced by this adapter, by design.
/// QTDE_ENTREGUE and VALOR_ENTREGUE are NEVER written — they are read fresh inside the transaction and
/// preserved as-is in the recomputed totals, exactly as the reference mechanism requires.
///
/// Homologation-only for now: <see cref="AllowedConnectionProfiles"/> restricts execution to
/// <see cref="WriteVerificationProfileSeeds.LinxDevelopment"/>. Production wiring (linx-production) is a
/// deliberately separate future step, not included in this capability.
/// </summary>
public sealed class PedGradeAdjustmentGovernedWriteAdapter(
    IConfiguration configuration,
    PedGradeAdjustmentRequest request,
    string connectionProfile = WriteVerificationProfileSeeds.LinxDevelopment)
    : IWriteExecutionAdapter, ISnapshotCapableAdapter
{
    public const string CapabilityId = "ped-grade-adjustment-write";
    public const string OwnerAgentId = "linx-erp-specialist-agent";
    public const string TableName = "COMPRAS_PRODUTO";

    private static readonly string[] SnapshotColumns =
    [
        "PEDIDO", "PRODUTO", "COR_PRODUTO",
        "CO1", "CO2", "CO3", "CO4", "CO5", "CO6",
        "CE1", "CE2", "CE3", "CE4", "CE5", "CE6",
        "QTDE_ORIGINAL", "QTDE_ENTREGAR", "QTDE_ENTREGUE",
        "VALOR_ORIGINAL", "VALOR_ENTREGAR", "VALOR_ENTREGUE",
        "CUSTO1",
    ];

    public string Capability => CapabilityId;
    public string OwnerAgent => OwnerAgentId;

    // Homologation-only: linx-production and wise are deliberately absent from this list. Production wiring
    // for this capability is a separate future step, not part of this PR.
    public IReadOnlyList<string> AllowedConnectionProfiles => [WriteVerificationProfileSeeds.LinxDevelopment];

    /// <summary>
    /// RestoreBeforeState: this is a pure quantity/value UPDATE on an EXISTING row (grade quantities and their
    /// derived totals) — no Insert or Delete is ever involved, and every field this adapter touches is fully
    /// reversible from a snapshot captured before the write. Unlike
    /// <see cref="GarantirFornecedorGovernedWriteAdapter"/> (NotSupported, a real "never destroy" business
    /// rule), there is no domain reason here that a restore-before-state rollback could not honor.
    /// </summary>
    public RollbackStrategy RollbackStrategy => RollbackStrategy.RestoreBeforeState;

    public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request2, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SomaLinxDryRunPreview(
            request2.Proposal.System, request2.Proposal.Environment, request2.Proposal.Resource, request2.Proposal.Operation,
            request2.Proposal.Fields, request2.Proposal.FilterSummary, request2.Proposal.ExpectedAffectedRows,
            request2.Proposal.Purpose, request2.ConnectionProfile, request2.PolicyDecision.RiskClassification,
            request2.PolicyDecision.Status, request2.ApprovalGrant is null ? "none" : "granted",
            request2.Proposal.Reversibility, request2.ExecutionMode,
            CredentialResolutionRequired: true, IdentityPermissionCheckRequired: true,
            SqlGenerated: false, ExternalExecutionPerformed: false));

    public async Task<WriteExecutionResult> ExecuteAsync(
        ToolGatewayRequest gatewayRequest,
        RecoveryPackageReceipt? recoveryPackage,
        CancellationToken cancellationToken = default)
    {
        // Guard clause: negative desired quantities are never valid — fail cleanly before touching the DB.
        if (request.Tam1 < 0 || request.Tam2 < 0 || request.Tam3 < 0 || request.Tam4 < 0 || request.Tam5 < 0 || request.Tam6 < 0)
        {
            return new WriteExecutionResult(false, 0, [], ["WRITE_FAILED", "NEGATIVE_GRADE_QUANTITY"],
                "Uma ou mais quantidades de grade desejadas (TAM1..TAM6) sao negativas.");
        }

        SqlConnection? connection = null;
        SqlTransaction? transaction = null;
        try
        {
            connection = await OpenAsync(cancellationToken);
            transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            // Re-read the current live values inside the transaction — never trust values passed in.
            var current = await ReadCurrentAsync(connection, transaction, cancellationToken);
            if (current is null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new WriteExecutionResult(false, 0, [], ["WRITE_FAILED", "ROW_NOT_FOUND"],
                    "Linha COMPRAS_PRODUTO nao encontrada para a chave informada (este adaptador nunca insere).");
            }

            var (qtdeEntregue, valorEntregue, custo1) = current.Value;
            var total = request.Total;
            var qtdeEntregar = total - qtdeEntregue;
            var valorOriginal = total * custo1;
            var valorEntregar = valorOriginal - valorEntregue;

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    UPDATE COMPRAS_PRODUTO SET
                      CO1=@s1, CO2=@s2, CO3=@s3, CO4=@s4, CO5=@s5, CO6=@s6,
                      CE1=@s1, CE2=@s2, CE3=@s3, CE4=@s4, CE5=@s5, CE6=@s6,
                      QTDE_ORIGINAL=@total,
                      QTDE_ENTREGAR=@qtdeEntregar,
                      VALOR_ORIGINAL=@valorOriginal,
                      VALOR_ENTREGAR=@valorEntregar
                    WHERE PEDIDO=@pedido AND PRODUTO=@produto AND COR_PRODUTO=@cor
                    """;
                AddKeyParameters(command);
                command.Parameters.Add(new SqlParameter("@s1", request.Tam1));
                command.Parameters.Add(new SqlParameter("@s2", request.Tam2));
                command.Parameters.Add(new SqlParameter("@s3", request.Tam3));
                command.Parameters.Add(new SqlParameter("@s4", request.Tam4));
                command.Parameters.Add(new SqlParameter("@s5", request.Tam5));
                command.Parameters.Add(new SqlParameter("@s6", request.Tam6));
                command.Parameters.Add(new SqlParameter("@total", total));
                command.Parameters.Add(new SqlParameter("@qtdeEntregar", qtdeEntregar));
                command.Parameters.Add(new SqlParameter("@valorOriginal", valorOriginal));
                command.Parameters.Add(new SqlParameter("@valorEntregar", valorEntregar));
                // Do NOT trust SqlCommand.ExecuteNonQueryAsync's returned count here: COMPRAS_PRODUTO carries a
                // pre-existing ERP trigger (PIT_BizTalk_COMPRAS_PRODUTO_U) that runs its own statements inside
                // the same UPDATE execution, which corrupts the ambient rows-affected count ADO.NET reports
                // (a well-known SQL Server/trigger gotcha, independent of NOCOUNT). Success is instead verified
                // by re-reading the row inside this same transaction and comparing against the desired values.
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            var verify = await ReadGradeAsync(connection, transaction, cancellationToken);
            if (verify is null ||
                verify.Value.Co1 != request.Tam1 || verify.Value.Co2 != request.Tam2 || verify.Value.Co3 != request.Tam3 ||
                verify.Value.Co4 != request.Tam4 || verify.Value.Co5 != request.Tam5 || verify.Value.Co6 != request.Tam6)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new WriteExecutionResult(false, 0, [], ["WRITE_FAILED", "UPDATE_NOT_APPLIED"],
                    "UPDATE nao surtiu efeito esperado: reconsulta dentro da transacao nao confere com os valores desejados.");
            }

            await using (var movimenta = connection.CreateCommand())
            {
                movimenta.Transaction = transaction;
                movimenta.CommandText = "EXEC LX_MOVIMENTA_COMPRAS_PA @PEDIDO";
                movimenta.Parameters.Add(new SqlParameter("@PEDIDO", request.Pedido));
                await movimenta.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var recalculo = connection.CreateCommand())
            {
                recalculo.Transaction = transaction;
                recalculo.CommandText = "EXEC LX_RECALCULO_RESERVA_MATERIAIS @PRODUTO=@produto, @XORDEM_PRODUCAO=@pedido";
                recalculo.Parameters.Add(new SqlParameter("@produto", request.Produto));
                recalculo.Parameters.Add(new SqlParameter("@pedido", request.Pedido));
                await recalculo.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new WriteExecutionResult(true, 1, [], ["LIVE_EXECUTION_COMPLETED"]);
        }
        catch (Exception)
        {
            if (transaction is not null)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); } catch { /* best effort */ }
            }

            // Sanitized: no connection string, stack trace or raw SQL text crosses this boundary.
            return new WriteExecutionResult(false, 0, [], ["WRITE_FAILED", "GRADE_ADJUSTMENT_EXECUTION_ERROR"],
                "Falha ao executar o ajuste de grade em COMPRAS_PRODUTO. Ver logs de infraestrutura para detalhes.");
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
            if (connection is not null) await connection.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT {string.Join(",", SnapshotColumns)} FROM {TableName} WHERE PEDIDO=@pedido AND PRODUTO=@produto AND COR_PRODUTO=@cor";
        AddKeyParameters(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var records = new List<IReadOnlyDictionary<string, string?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var record = new Dictionary<string, string?>();
            foreach (var column in SnapshotColumns)
            {
                var value = reader[column];
                record[column] = value is DBNull ? null : Convert.ToString(value)?.Trim();
            }

            records.Add(record);
        }

        return [new RecoveryDataSet(TableName, records)];
    }

    private async Task<(int QtdeEntregue, decimal ValorEntregue, decimal Custo1)?> ReadCurrentAsync(
        SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "SELECT QTDE_ENTREGUE, VALOR_ENTREGUE, CUSTO1 FROM COMPRAS_PRODUTO WITH (UPDLOCK, ROWLOCK) " +
            "WHERE PEDIDO=@pedido AND PRODUTO=@produto AND COR_PRODUTO=@cor";
        AddKeyParameters(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var qtdeEntregue = reader["QTDE_ENTREGUE"] is DBNull ? 0 : Convert.ToInt32(reader["QTDE_ENTREGUE"]);
        var valorEntregue = reader["VALOR_ENTREGUE"] is DBNull ? 0m : Convert.ToDecimal(reader["VALOR_ENTREGUE"]);
        var custo1 = reader["CUSTO1"] is DBNull ? 0m : Convert.ToDecimal(reader["CUSTO1"]);
        return (qtdeEntregue, valorEntregue, custo1);
    }

    private async Task<(int Co1, int Co2, int Co3, int Co4, int Co5, int Co6)?> ReadGradeAsync(
        SqlConnection connection, SqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "SELECT CO1, CO2, CO3, CO4, CO5, CO6 FROM COMPRAS_PRODUTO WHERE PEDIDO=@pedido AND PRODUTO=@produto AND COR_PRODUTO=@cor";
        AddKeyParameters(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return (Convert.ToInt32(reader["CO1"]), Convert.ToInt32(reader["CO2"]), Convert.ToInt32(reader["CO3"]),
            Convert.ToInt32(reader["CO4"]), Convert.ToInt32(reader["CO5"]), Convert.ToInt32(reader["CO6"]));
    }

    private void AddKeyParameters(SqlCommand command)
    {
        command.Parameters.Add(new SqlParameter("@pedido", request.Pedido));
        command.Parameters.Add(new SqlParameter("@produto", request.Produto));
        command.Parameters.Add(new SqlParameter("@cor", request.CorProduto));
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var profile = connectionProfile == WriteVerificationProfileSeeds.LinxDevelopment
            ? LinxConnectionProfiles.Development
            : throw new InvalidOperationException(
                $"PedGradeAdjustmentGovernedWriteAdapter so aceita o profile '{WriteVerificationProfileSeeds.LinxDevelopment}' nesta versao (homologacao). Profile recebido: '{connectionProfile}'.");

        var connectionString = LinxConnectionStringResolver.Resolve(configuration, profile);
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
