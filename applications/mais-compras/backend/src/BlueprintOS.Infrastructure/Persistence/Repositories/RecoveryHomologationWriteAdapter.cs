using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>
/// A generic, disposable write adapter for <c>BLUEPRINTOS_RECOVERY_HOMOLOGATION</c> — a table that exists only
/// in SOMA_DESENV, created solely to homologate the Production Write Verification &amp; Recovery Policy without
/// exercising any real ERP/business rule (see agents/DATABASE_CONNECTION_POLICY.md §24). It carries no supplier,
/// order, or any domain semantics: a single row (ID, VALOR) whose value toggles between two known strings.
///
/// Declares <see cref="RollbackStrategy.RestoreBeforeState"/> — unlike
/// <see cref="GarantirFornecedorGovernedWriteAdapter"/> (NotSupported, a real business rule), this capability
/// has no domain restriction, so the generic mechanism applies: the physical operation (insert/update/delete)
/// is decided by the caller (<c>RollbackOrchestrator</c> for a rollback, this adapter's own constructor for a
/// forward write) from what the recorded before/after state objectively requires, never assumed.
/// </summary>
public sealed class RecoveryHomologationWriteAdapter(
    IConfiguration configuration,
    string id,
    string? targetValue) : IWriteExecutionAdapter, ISnapshotCapableAdapter
{
    public const string CapabilityId = "recovery-homologation-write";
    public const string OwnerAgentId = "linx-database-specialist-agent";
    public const string TableName = "BLUEPRINTOS_RECOVERY_HOMOLOGATION";

    public string Capability => CapabilityId;
    public string OwnerAgent => OwnerAgentId;
    public IReadOnlyList<string> AllowedConnectionProfiles => [WriteVerificationProfileSeeds.LinxDevelopment];
    public RollbackStrategy RollbackStrategy => RollbackStrategy.RestoreBeforeState;

    public Task<SomaLinxDryRunPreview> DryRunAsync(ToolGatewayRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SomaLinxDryRunPreview(
            request.Proposal.System, request.Proposal.Environment, request.Proposal.Resource, request.Proposal.Operation,
            request.Proposal.Fields, request.Proposal.FilterSummary, request.Proposal.ExpectedAffectedRows,
            request.Proposal.Purpose, request.ConnectionProfile, request.PolicyDecision.RiskClassification,
            request.PolicyDecision.Status, "granted", request.Proposal.Reversibility, request.ExecutionMode,
            true, true, false, false));

    /// <summary>
    /// Performs the physical operation the GOVERNED proposal declares (<see cref="ActionProposal.Operation"/>),
    /// never one this adapter infers on its own — <c>RollbackOrchestrator.BuildEquivalentProposal</c> is the
    /// single place that determines Insert/Update/Delete from the recorded before/current state, and this
    /// adapter only carries out what governance already decided and approved.
    /// </summary>
    public async Task<WriteExecutionResult> ExecuteAsync(ToolGatewayRequest request, RecoveryPackageReceipt? recoveryPackage, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            int rows;
            switch (request.Proposal.Operation)
            {
                case ActionOperation.Delete:
                    rows = await ExecuteNonQueryAsync(connection, transaction,
                        $"DELETE FROM [dbo].[{TableName}] WHERE [ID] = @id", cancellationToken);
                    break;
                case ActionOperation.Insert:
                    rows = await ExecuteNonQueryAsync(connection, transaction,
                        $"INSERT INTO [dbo].[{TableName}] ([ID], [VALOR]) VALUES (@id, @valor)", cancellationToken, targetValue);
                    break;
                default:
                    rows = await ExecuteNonQueryAsync(connection, transaction,
                        $"UPDATE [dbo].[{TableName}] SET [VALOR] = @valor WHERE [ID] = @id", cancellationToken, targetValue);
                    break;
            }

            await transaction.CommitAsync(cancellationToken);
            return new WriteExecutionResult(true, rows, [], ["LIVE_EXECUTION_COMPLETED"]);
        }
        catch (Exception ex)
        {
            try { await transaction.RollbackAsync(CancellationToken.None); } catch { /* best effort */ }
            return new WriteExecutionResult(false, 0, [], [ex.Message]);
        }
    }

    public async Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(IReadOnlyList<string> businessKeys, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT [ID], [VALOR] FROM [dbo].[{TableName}] WHERE [ID] = @id";
        command.Parameters.Add(new SqlParameter("@id", id));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var records = new List<IReadOnlyDictionary<string, string?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new Dictionary<string, string?>
            {
                ["ID"] = Convert.ToString(reader["ID"])?.Trim(),
                ["VALOR"] = reader["VALOR"] is DBNull ? null : Convert.ToString(reader["VALOR"])?.Trim(),
            });
        }

        return [new RecoveryDataSet(TableName, records)];
    }

    private async Task<int> ExecuteNonQueryAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken, string? valor = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@id", id));
        if (valor is not null) command.Parameters.Add(new SqlParameter("@valor", valor));
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connectionString = LinxConnectionStringResolver.Resolve(configuration, LinxConnectionProfiles.Development);
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
