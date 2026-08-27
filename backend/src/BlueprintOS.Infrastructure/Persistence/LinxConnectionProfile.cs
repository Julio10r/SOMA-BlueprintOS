namespace BlueprintOS.Infrastructure.Persistence;

/// <summary>Ambiente lógico do ERP Linx/SOMA. Nunca infira um a partir do outro: servidor, banco e
/// credencial são distintos entre Development e Production (ver agents/DATABASE_CONNECTION_POLICY.md).</summary>
public enum LinxEnvironment
{
    Development,
    Production,
}

/// <summary>Profile lógico de conexão a um banco DEV/PROD (ERP Linx/SOMA ou +Compras): identifica
/// ambiente, chave de User Secrets e o servidor/banco esperados para proteção contra environment
/// mismatch. Nunca carrega credencial — usuário e senha permanecem exclusivamente em User
/// Secrets/variável de ambiente locais. Dois profiles podem legitimamente compartilhar a mesma
/// identidade local (ex.: <see cref="LinxConnectionProfiles.Development"/> e
/// <see cref="LinxConnectionProfiles.MaisComprasDevelopment"/> — mesmo servidor DEV, mesma
/// identidade, bancos diferentes) sem que o profile em si carregue ou duplique o segredo.</summary>
public sealed record LinxConnectionProfile(
    string Label,
    LinxEnvironment Environment,
    string ConnectionName,
    string ExpectedServer,
    string ExpectedDatabase,
    bool VpnRequired);

/// <summary>Profiles canônicos de ambiente DEV/PROD (agents/DATABASE_CONNECTION_POLICY.md § Profiles).
/// Servidor e banco são metadados lógicos versionáveis; a credencial nunca aparece aqui.</summary>
public static class LinxConnectionProfiles
{
    public static readonly LinxConnectionProfile Development = new(
        Label: "ERP Linx SOMA_DESENV (Development)",
        Environment: LinxEnvironment.Development,
        ConnectionName: "LinxDevelopmentConnection",
        ExpectedServer: "192.168.9.98",
        ExpectedDatabase: "SOMA_DESENV",
        VpnRequired: true);

    /// <summary>Endpoint corrigido em 2026-08-27 com base em evidência real de conexão bem-sucedida
    /// (@@SERVERNAME/CONNECTIONPROPERTY('local_net_address') = SRV-SOMADB / 192.168.9.200, porta 1433,
    /// TCP, sem instância nomeada). O valor anterior (192.168.0.200) nunca foi o endpoint SQL real de
    /// produção — nao havia porta bloqueada por firewall, o host configurado estava incorreto. Ver
    /// docs/audits/LinxProductionEndpointCorrectionV1.md.</summary>
    public static readonly LinxConnectionProfile Production = new(
        Label: "ERP Linx SOMA (Production)",
        Environment: LinxEnvironment.Production,
        ConnectionName: "LinxProductionConnection",
        ExpectedServer: "192.168.9.200",
        ExpectedDatabase: "SOMA",
        VpnRequired: true);

    /// <summary>Mesmo servidor DEV (192.168.9.98) do profile <see cref="Development"/>, banco
    /// diferente (+Compras). Resolve a mesma chave de identidade local
    /// (<c>ConnectionStrings:MaisComprasConnection</c>) já usada pelo restante da aplicação — não é
    /// um novo segredo, apenas o profile lógico formalizado para participar da mesma proteção de
    /// environment mismatch dos demais bancos DEV/PROD.</summary>
    public static readonly LinxConnectionProfile MaisComprasDevelopment = new(
        Label: "+Compras (Development)",
        Environment: LinxEnvironment.Development,
        ConnectionName: "MaisComprasConnection",
        ExpectedServer: "192.168.9.98",
        ExpectedDatabase: "MAISCOMPRAS",
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
