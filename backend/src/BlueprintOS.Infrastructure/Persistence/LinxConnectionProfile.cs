namespace BlueprintOS.Infrastructure.Persistence;

/// <summary>Ambiente lógico do ERP Linx/SOMA. Nunca infira um a partir do outro: servidor, banco e
/// credencial são distintos entre Development e Production (ver agents/DATABASE_CONNECTION_POLICY.md).</summary>
public enum LinxEnvironment
{
    Development,
    Production,
}

/// <summary>Profile lógico de conexão ao ERP Linx/SOMA: identifica ambiente, chave de User Secrets e o
/// servidor/banco esperados para proteção contra environment mismatch. Nunca carrega credencial —
/// usuário e senha permanecem exclusivamente em User Secrets/variável de ambiente locais.</summary>
public sealed record LinxConnectionProfile(
    LinxEnvironment Environment,
    string ConnectionName,
    string ExpectedServer,
    string ExpectedDatabase,
    bool VpnRequired)
{
    public string Label => Environment == LinxEnvironment.Development
        ? "ERP Linx SOMA_DESENV (Development)"
        : "ERP Linx SOMA (Production)";
}

/// <summary>Profiles canônicos de ambiente Linx/SOMA (agents/DATABASE_CONNECTION_POLICY.md § Profiles).
/// Servidor e banco são metadados lógicos versionáveis; a credencial nunca aparece aqui.</summary>
public static class LinxConnectionProfiles
{
    public static readonly LinxConnectionProfile Development = new(
        Environment: LinxEnvironment.Development,
        ConnectionName: "LinxDevelopmentConnection",
        ExpectedServer: "192.168.9.98",
        ExpectedDatabase: "SOMA_DESENV",
        VpnRequired: true);

    public static readonly LinxConnectionProfile Production = new(
        Environment: LinxEnvironment.Production,
        ConnectionName: "LinxProductionConnection",
        ExpectedServer: "192.168.0.200",
        ExpectedDatabase: "SOMA",
        VpnRequired: true);

    /// <summary>Chave de connection string legada, pré-separação DEV/PROD. Mantida apenas como fallback
    /// de compatibilidade para <see cref="Development"/> — DEPRECATED, migrar para
    /// <c>ConnectionStrings:LinxDevelopmentConnection</c>. Nunca usada para Production.</summary>
    public const string LegacyErpConnectionName = "ErpConnection";

    public static LinxConnectionProfile Resolve(LinxEnvironment environment) => environment switch
    {
        LinxEnvironment.Development => Development,
        LinxEnvironment.Production => Production,
        _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Unknown Linx environment."),
    };
}
