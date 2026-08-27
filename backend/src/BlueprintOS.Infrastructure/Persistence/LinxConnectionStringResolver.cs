using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Infrastructure.Persistence;

/// <summary>Ponto único de resolução de connection string por profile DEV/PROD, reaproveitado por todo
/// consumidor Linx/SOMA/+Compras — elimina a duplicação de guarda de placeholder e de checagem de
/// catálogo que antes existia em cada reader/adapter. Nunca loga, retorna ou expõe a connection string
/// fora deste processo além do valor necessário para abrir a conexão SQL; nunca contorna
/// <see cref="LinxConnectionProfile"/> — todo consumidor declara explicitamente qual profile usa.</summary>
public static class LinxConnectionStringResolver
{
    /// <summary>Resolve e valida a connection string do profile informado. Lança
    /// <see cref="InvalidOperationException"/> se não configurada (nunca conecta com placeholder) ou se
    /// o servidor/banco resolvidos não baterem com o profile esperado (environment mismatch), sem nunca
    /// incluir a connection string na mensagem de exceção.</summary>
    public static string Resolve(IConfiguration configuration, LinxConnectionProfile profile)
    {
        var connectionString = configuration.GetConnectionString(profile.ConnectionName);

        if (IsUnset(connectionString)
            && profile.Environment == LinxEnvironment.Development
            && profile.ConnectionName == LinxConnectionProfiles.Development.ConnectionName)
        {
            // Fallback de compatibilidade DEPRECATED — apenas para o profile Linx Development canônico.
            var legacy = configuration.GetConnectionString(LinxConnectionProfiles.LegacyErpConnectionName);
            if (!IsUnset(legacy))
            {
                connectionString = legacy;
            }
        }

        if (IsUnset(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{profile.ConnectionName} deve ser configurada via User Secrets ou variável de ambiente (profile {profile.Environment}: {profile.Label}).");
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!Matches(builder, profile))
        {
            throw new InvalidOperationException(
                $"Environment mismatch: profile {profile.Environment} ({profile.Label}) espera servidor '{profile.ExpectedServer}' / banco '{profile.ExpectedDatabase}'. Bloqueado antes de abrir a conexão.");
        }

        return connectionString!;
    }

    private static bool IsUnset(string? connectionString) =>
        string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal);

    private static bool Matches(SqlConnectionStringBuilder builder, LinxConnectionProfile profile) =>
        builder.DataSource.Contains(profile.ExpectedServer, StringComparison.OrdinalIgnoreCase)
        && string.Equals(builder.InitialCatalog, profile.ExpectedDatabase, StringComparison.OrdinalIgnoreCase);
}
