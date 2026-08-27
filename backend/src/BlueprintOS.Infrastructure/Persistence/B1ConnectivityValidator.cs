using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Infrastructure.Persistence;

/// <summary>Validação read-only de conectividade a bancos externos (+Compras, ERP Linx SOMA_DESENV).
/// Nunca lê, loga ou retorna a connection string; expõe apenas Server/Database (nomes lógicos, não
/// segredo) e, em caso de sucesso, a identidade efetiva de login resolvida pelo próprio banco
/// (<c>SUSER_SNAME()</c>) — nunca a credencial usada para obtê-la. Único comando de escrita: nenhum;
/// os dois comandos emitidos são <c>SELECT 1</c> e <c>SELECT SUSER_SNAME()</c>.</summary>
public sealed class B1ConnectivityValidator(IConfiguration configuration)
{
    public Task<DatabaseConnectivityResult> ValidateMaisComprasAsync(CancellationToken cancellationToken = default) =>
        ValidateAsync("+Compras", "MaisComprasConnection", cancellationToken);

    public Task<DatabaseConnectivityResult> ValidateErpAsync(CancellationToken cancellationToken = default) =>
        ValidateAsync("ERP SOMA_DESENV", "ErpConnection", cancellationToken);

    private async Task<DatabaseConnectivityResult> ValidateAsync(string label, string connectionName, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal))
        {
            return DatabaseConnectivityResult.NotConfigured(label, connectionName);
        }

        SqlConnectionStringBuilder? builder = null;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using (var probe = connection.CreateCommand())
            {
                probe.CommandText = "SELECT 1;";
                probe.CommandTimeout = 15;
                await probe.ExecuteScalarAsync(cancellationToken);
            }

            string? effectiveIdentity = null;
            try
            {
                await using var identity = connection.CreateCommand();
                identity.CommandText = "SELECT SUSER_SNAME();";
                identity.CommandTimeout = 15;
                effectiveIdentity = (await identity.ExecuteScalarAsync(cancellationToken)) as string;
            }
            catch
            {
                // A identidade efetiva é informativa; falhar em obtê-la não deve derrubar uma conexão já validada por SELECT 1.
            }

            return DatabaseConnectivityResult.Success(label, builder.DataSource, builder.InitialCatalog, effectiveIdentity);
        }
        catch (SqlException exception) when (IsPermissionDenied(exception))
        {
            return DatabaseConnectivityResult.PermissionDenied(label, builder?.DataSource, builder?.InitialCatalog, exception);
        }
        catch (Exception exception)
        {
            return DatabaseConnectivityResult.Failure(label, builder?.DataSource, builder?.InitialCatalog, exception);
        }
    }

    /// <summary>Classes de erro do SQL Server tipicamente associadas a autenticação/autorização negada
    /// (login falhou, permissão negada no objeto/comando, usuário sem acesso ao banco) — nunca tratadas
    /// como "banco fora do ar", para que o Agent nunca tente contornar com privilégio elevado.</summary>
    private static bool IsPermissionDenied(SqlException exception) =>
        exception.Number is 18456 or 229 or 230 or 262 or 4060;
}

public enum ConnectivityStatus
{
    Ready,
    NotConfigured,
    Failed,
    PermissionDenied,
}

public sealed record DatabaseConnectivityResult(
    string Label,
    ConnectivityStatus Status,
    string? Server,
    string? Database,
    string? EffectiveIdentity,
    Exception? Exception)
{
    public bool IsSuccess => Status == ConnectivityStatus.Ready;

    public static DatabaseConnectivityResult Success(string label, string? server, string? database, string? effectiveIdentity) =>
        new(label, ConnectivityStatus.Ready, server, database, effectiveIdentity, null);

    public static DatabaseConnectivityResult NotConfigured(string label, string connectionName) =>
        new(label, ConnectivityStatus.NotConfigured, null, null, null,
            new InvalidOperationException($"ConnectionStrings:{connectionName} is not configured."));

    public static DatabaseConnectivityResult Failure(string label, string? server, string? database, Exception exception) =>
        new(label, ConnectivityStatus.Failed, server, database, null, exception);

    public static DatabaseConnectivityResult PermissionDenied(string label, string? server, string? database, Exception exception) =>
        new(label, ConnectivityStatus.PermissionDenied, server, database, null, exception);
}
