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
        ValidateAsync(LinxConnectionProfiles.MaisComprasDevelopment.Label, LinxConnectionProfiles.MaisComprasDevelopment.ConnectionName,
            LinxConnectionProfiles.MaisComprasDevelopment, cancellationToken);

    /// <summary>Mantido por compatibilidade — resolve o profile Development. Prefira
    /// <see cref="ValidateErpAsync(LinxEnvironment, CancellationToken)"/> explícito.
    /// DEPRECATED: lê primeiro <c>ConnectionStrings:LinxDevelopmentConnection</c>; se ausente, cai para a
    /// chave legada <c>ConnectionStrings:ErpConnection</c>.</summary>
    public Task<DatabaseConnectivityResult> ValidateErpAsync(CancellationToken cancellationToken = default) =>
        ValidateErpAsync(LinxEnvironment.Development, cancellationToken);

    /// <summary>Valida read-only o profile Linx/SOMA do ambiente informado, com proteção determinística
    /// contra environment mismatch: o servidor/banco resolvidos pela connection string configurada devem
    /// bater com o esperado pelo profile antes de qualquer tentativa de abrir conexão de rede.</summary>
    public Task<DatabaseConnectivityResult> ValidateErpAsync(LinxEnvironment environment, CancellationToken cancellationToken = default)
    {
        var profile = LinxConnectionProfiles.Resolve(environment);
        var connectionName = profile.ConnectionName;
        var connectionString = configuration.GetConnectionString(connectionName);
        if ((string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal))
            && environment == LinxEnvironment.Development)
        {
            // Fallback de compatibilidade: chave legada ErpConnection, DEPRECATED, apontava para SOMA_DESENV.
            var legacy = configuration.GetConnectionString(LinxConnectionProfiles.LegacyErpConnectionName);
            if (!string.IsNullOrWhiteSpace(legacy) && !legacy.StartsWith("__SET_", StringComparison.Ordinal))
            {
                connectionString = legacy;
                connectionName = LinxConnectionProfiles.LegacyErpConnectionName;
            }
        }

        return ValidateAsync(profile.Label, connectionName, profile, cancellationToken, connectionString);
    }

    private async Task<DatabaseConnectivityResult> ValidateAsync(
        string label,
        string connectionName,
        LinxConnectionProfile? expectedProfile,
        CancellationToken cancellationToken,
        string? connectionStringOverride = null)
    {
        var connectionString = connectionStringOverride ?? configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal))
        {
            return DatabaseConnectivityResult.NotConfigured(label, connectionName);
        }

        SqlConnectionStringBuilder? builder = null;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);

            if (expectedProfile is not null && IsEnvironmentMismatch(builder, expectedProfile))
            {
                return DatabaseConnectivityResult.EnvironmentMismatch(label, builder.DataSource, builder.InitialCatalog, expectedProfile);
            }

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
        catch (Exception exception) when (expectedProfile is { VpnRequired: true } && IsNetworkUnreachable(exception))
        {
            return DatabaseConnectivityResult.VpnRequired(label, builder?.DataSource, builder?.InitialCatalog, exception);
        }
        catch (Exception exception)
        {
            return DatabaseConnectivityResult.Failure(label, builder?.DataSource, builder?.InitialCatalog, exception);
        }
    }

    /// <summary>Compara servidor/banco resolvidos da connection string configurada contra o profile
    /// esperado, sem exigir rede — bloqueia determinísticamente antes de qualquer tentativa de conexão
    /// quando um profile Development aponta para servidor/banco de Production, ou vice-versa.</summary>
    private static bool IsEnvironmentMismatch(SqlConnectionStringBuilder builder, LinxConnectionProfile expectedProfile)
    {
        var serverMatches = builder.DataSource.Contains(expectedProfile.ExpectedServer, StringComparison.OrdinalIgnoreCase);
        var databaseMatches = string.Equals(builder.InitialCatalog, expectedProfile.ExpectedDatabase, StringComparison.OrdinalIgnoreCase);
        return !serverMatches || !databaseMatches;
    }

    /// <summary>Classes de erro do SQL Server tipicamente associadas a autenticação/autorização negada
    /// (login falhou, permissão negada no objeto/comando, usuário sem acesso ao banco) — nunca tratadas
    /// como "banco fora do ar", para que o Agent nunca tente contornar com privilégio elevado.</summary>
    private static bool IsPermissionDenied(SqlException exception) =>
        exception.Number is 18456 or 229 or 230 or 262 or 4060;

    /// <summary>Distingue "VPN desconectada / rede inacessível" de "credencial inválida": erros de
    /// resolução de rede/timeout de handshake nunca devem ser classificados como falha de credencial.</summary>
    private static bool IsNetworkUnreachable(Exception exception) => exception switch
    {
        SqlException sql => sql.Number is 53 or -2 or -1 or 2 or 258 or 10060,
        System.Net.Sockets.SocketException => true,
        TimeoutException => true,
        _ => false,
    };
}

public enum ConnectivityStatus
{
    Ready,
    NotConfigured,
    Failed,
    PermissionDenied,
    EnvironmentMismatch,
    VpnRequired,
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

    public static DatabaseConnectivityResult EnvironmentMismatch(string label, string? server, string? database, LinxConnectionProfile expectedProfile) =>
        new(label, ConnectivityStatus.EnvironmentMismatch, server, database, null,
            new InvalidOperationException(
                $"Environment mismatch: profile {expectedProfile.Environment} expects server '{expectedProfile.ExpectedServer}' / database '{expectedProfile.ExpectedDatabase}', but the configured connection string resolves to a different target. Blocked before opening any connection."));

    public static DatabaseConnectivityResult VpnRequired(string label, string? server, string? database, Exception exception) =>
        new(label, ConnectivityStatus.VpnRequired, server, database, null, exception);
}
