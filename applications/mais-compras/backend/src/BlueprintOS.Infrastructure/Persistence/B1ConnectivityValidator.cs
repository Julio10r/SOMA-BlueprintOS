using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Infrastructure.Persistence;

/// <summary>Validação read-only de conectividade a bancos externos (+Compras, ERP Linx SOMA_DESENV).
/// Nunca lê, loga ou retorna a connection string; expõe apenas Server/Database (nomes lógicos, não
/// segredo) e, em caso de sucesso, a identidade efetiva de login resolvida pelo próprio banco
/// (<c>SUSER_SNAME()</c>) — nunca a credencial usada para obtê-la. Único comando de escrita: nenhum;
/// os dois comandos emitidos são <c>SELECT 1</c> e <c>SELECT SUSER_SNAME()</c>.</summary>
public sealed class B1ConnectivityValidator
{
    /// <summary>Intervalo antes da única tentativa automática de reconexão quando a falha é classificada
    /// como conectividade indisponível (nunca para credencial/permissão/mismatch/not-configured). VPN
    /// corporativa oscila mesmo já conectada; um retry único e rápido absorve essa instabilidade sem
    /// declarar "VPN desconectada" prematuramente e sem criar um loop de retries.</summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(750);

    private readonly IConfiguration configuration;
    private readonly ISqlConnectivityProbe probe;

    public B1ConnectivityValidator(IConfiguration configuration, ISqlConnectivityProbe? probe = null)
    {
        this.configuration = configuration;
        this.probe = probe ?? new SqlConnectivityProbe();
    }


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

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception exception)
        {
            return DatabaseConnectivityResult.Failure(label, null, null, exception);
        }

        if (expectedProfile is not null && IsEnvironmentMismatch(builder, expectedProfile))
        {
            return DatabaseConnectivityResult.EnvironmentMismatch(label, builder.DataSource, builder.InitialCatalog, expectedProfile);
        }

        try
        {
            var effectiveIdentity = await probe.ProbeAsync(connectionString, cancellationToken);
            return DatabaseConnectivityResult.Success(label, builder.DataSource, builder.InitialCatalog, effectiveIdentity);
        }
        catch (Exception exception) when (IsPermissionDenied(exception))
        {
            // Nunca elegível para retry: credencial/permissão negada não é instabilidade de rede.
            return DatabaseConnectivityResult.PermissionDenied(label, builder.DataSource, builder.InitialCatalog, exception);
        }
        catch (Exception exception) when (IsNetworkUnreachable(exception))
        {
            // Exatamente 1 tentativa automática adicional — VPN corporativa conectada pode oscilar
            // momentaneamente; isto não prova "VPN desconectada", então não presumimos a causa.
            await Task.Delay(RetryDelay, cancellationToken);
            try
            {
                var effectiveIdentity = await probe.ProbeAsync(connectionString, cancellationToken);
                return DatabaseConnectivityResult.Success(label, builder.DataSource, builder.InitialCatalog, effectiveIdentity) with { RecoveredAfterRetry = true };
            }
            catch (Exception retryException) when (IsPermissionDenied(retryException))
            {
                return DatabaseConnectivityResult.PermissionDenied(label, builder.DataSource, builder.InitialCatalog, retryException);
            }
            catch (Exception retryException) when (IsNetworkUnreachable(retryException))
            {
                // Segunda tentativa também falhou por conectividade — parar aqui, sem novo retry.
                return DatabaseConnectivityResult.ConnectivityUnavailable(label, builder.DataSource, builder.InitialCatalog, retryException);
            }
            catch (Exception retryException)
            {
                return DatabaseConnectivityResult.Failure(label, builder.DataSource, builder.InitialCatalog, retryException);
            }
        }
        catch (Exception exception)
        {
            return DatabaseConnectivityResult.Failure(label, builder.DataSource, builder.InitialCatalog, exception);
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
    /// como "banco fora do ar", para que o Agent nunca tente contornar com privilégio elevado. Nunca
    /// elegível para o retry único de conectividade.</summary>
    private static bool IsPermissionDenied(Exception exception) => exception switch
    {
        SqlException sql => sql.Number is 18456 or 229 or 230 or 262 or 4060,
        ISimulatedSqlFailure simulated => simulated.IsPermissionDenied,
        _ => false,
    };

    /// <summary>Distingue "VPN desconectada / rede inacessível" de "credencial inválida": erros de
    /// resolução de rede/timeout de handshake nunca devem ser classificados como falha de credencial.
    /// O driver SqlClient frequentemente reporta indisponibilidade de rede/servidor com
    /// <c>SqlException.Number == 0</c> (sem código SQL nativo — a conexão TCP nunca chegou a existir),
    /// então a mensagem também é inspecionada para os textos característicos desse cenário.</summary>
    private static bool IsNetworkUnreachable(Exception exception) => exception switch
    {
        SqlException { Number: 53 or -2 or -1 or 2 or 258 or 10060 } => true,
        SqlException { Number: 0 } sql when IsNetworkUnreachableMessage(sql.Message) => true,
        System.Net.Sockets.SocketException => true,
        TimeoutException => true,
        _ => false,
    };

    private static bool IsNetworkUnreachableMessage(string message) =>
        message.Contains("network-related", StringComparison.OrdinalIgnoreCase)
        || message.Contains("TCP Provider", StringComparison.OrdinalIgnoreCase)
        || message.Contains("was not found or was not accessible", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Seam de teste: exceções de fake/teste implementam esta interface para se anunciar como
/// "permissão negada" sem precisar construir um <see cref="SqlException"/> real (cujos construtores são
/// internos ao driver Microsoft.Data.SqlClient). Nunca implementada por código de produção — a
/// classificação real usa sempre <see cref="SqlException.Number"/>.</summary>
public interface ISimulatedSqlFailure
{
    bool IsPermissionDenied { get; }
}

/// <summary>Seam de teste: encapsula a única operação de rede real (abrir conexão + <c>SELECT 1</c> +
/// <c>SELECT SUSER_SNAME()</c>) para que o retry único de <see cref="B1ConnectivityValidator"/> possa
/// ser exercitado em teste sem depender de um SQL Server real ou de VPN.</summary>
public interface ISqlConnectivityProbe
{
    /// <summary>Abre a conexão e executa a prova read-only. Retorna a identidade efetiva
    /// (<c>SUSER_SNAME()</c>), ou <c>null</c> se ela não puder ser obtida. Lança em qualquer falha de
    /// conexão/permissão — nunca engole a exceção original.</summary>
    Task<string?> ProbeAsync(string connectionString, CancellationToken cancellationToken);
}

internal sealed class SqlConnectivityProbe : ISqlConnectivityProbe
{
    public async Task<string?> ProbeAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var probe = connection.CreateCommand())
        {
            probe.CommandText = "SELECT 1;";
            probe.CommandTimeout = 15;
            await probe.ExecuteScalarAsync(cancellationToken);
        }

        try
        {
            await using var identity = connection.CreateCommand();
            identity.CommandText = "SELECT SUSER_SNAME();";
            identity.CommandTimeout = 15;
            return (await identity.ExecuteScalarAsync(cancellationToken)) as string;
        }
        catch
        {
            // A identidade efetiva é informativa; falhar em obtê-la não deve derrubar uma conexão já validada por SELECT 1.
            return null;
        }
    }
}

public enum ConnectivityStatus
{
    Ready,
    NotConfigured,
    Failed,
    PermissionDenied,
    EnvironmentMismatch,
    /// <summary>Rede/servidor inacessível após a tentativa inicial e exatamente 1 retry automático.
    /// Não é um diagnóstico de causa (VPN desconectada, firewall, servidor fora do ar) — apenas o fato
    /// observável de que a conectividade não pôde ser restabelecida em uma tentativa adicional.</summary>
    ConnectivityUnavailable,
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

    /// <summary>True quando o resultado Ready só foi alcançado após a tentativa inicial falhar por
    /// conectividade e o retry único ter funcionado. Informativo — não deve incomodar o usuário além de
    /// um registro discreto; o status final continua Ready.</summary>
    public bool RecoveredAfterRetry { get; init; }

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

    public static DatabaseConnectivityResult ConnectivityUnavailable(string label, string? server, string? database, Exception exception) =>
        new(label, ConnectivityStatus.ConnectivityUnavailable, server, database, null, exception);
}
