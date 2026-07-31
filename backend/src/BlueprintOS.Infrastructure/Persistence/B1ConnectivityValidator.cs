using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Infrastructure.Persistence;

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
            return DatabaseConnectivityResult.Failure(label, null, null,
                new InvalidOperationException($"ConnectionStrings:{connectionName} is not configured."));
        }

        SqlConnectionStringBuilder? builder = null;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.CommandTimeout = 15;
            await command.ExecuteScalarAsync(cancellationToken);
            return DatabaseConnectivityResult.Success(label, builder.DataSource, builder.InitialCatalog);
        }
        catch (Exception exception)
        {
            return DatabaseConnectivityResult.Failure(label, builder?.DataSource, builder?.InitialCatalog, exception);
        }
    }
}

public sealed record DatabaseConnectivityResult(
    string Label,
    bool IsSuccess,
    string? Server,
    string? Database,
    Exception? Exception)
{
    public static DatabaseConnectivityResult Success(string label, string? server, string? database) =>
        new(label, true, server, database, null);

    public static DatabaseConnectivityResult Failure(string label, string? server, string? database, Exception exception) =>
        new(label, false, server, database, exception);
}
