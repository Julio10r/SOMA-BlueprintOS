using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Identity;
using BlueprintOS.Api.Middleware;
using BlueprintOS.Api.Negotiations;
using BlueprintOS.Api.Suppliers;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

if (args.Length > 0 && args[0] == "publish")
{
    return await RunPublicationEngineAsync(args);
}

if (args.Length > 0 && (args[0] == "publish-docs" || args[0] == "publish-executive-blueprint"))
{
    Console.Error.WriteLine(
        $"O comando '{args[0]}' foi removido (ADR-0019). Use 'publish': ele descobre todo docs/ e publica em dist/, sem lógica por audiência.");
    return 1;
}

if (args.Length > 0 && args[0] == "migrate")
{
    return await RunMigrationsAsync();
}

if (args.Length > 0 && args[0] == "validate-maiscompras")
{
    return await ValidateMaisComprasAsync();
}

if (args.Length > 0 && args[0] == "validate-b1-connectivity")
{
    return await ValidateB1ConnectivityAsync();
}

if (args.Length > 0 && args[0] == "probe-erp-suppliers")
{
    return await ProbeErpSuppliersAsync(args);
}

if (args.Length > 0 && args[0] == "probe-erp-supplier-integrity")
{
    return await ProbeErpSupplierIntegrityAsync();
}

var builder = WebApplication.CreateBuilder(args);

// CORS — libera a origem do frontend (+Compras Web) em dev/demo.
// Em producao, configure "Cors:AllowedOrigins" via appsettings ou variavel
// de ambiente com a lista real de origins; nunca usar "*" com credenciais.
const string FrontendCorsPolicy = "FrontendCorsPolicy";
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173" };

// Services
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// Seleção do adaptador de identidade exclusivamente por IHostEnvironment — nunca por
// appsettings/feature flag (security-design-auth-o1.4.md, §17.4). DevelopmentRequestIdentity
// (ADR-0011) permanece disponível apenas em Development; fora dele, a sessão de cookie real.
//
// Secure-by-default (O1.4.2.1, Etapa 3): a partir daqui, TODO endpoint exige autenticação por
// padrão via AuthorizationOptions.FallbackPolicy — anônimo é exceção explícita (.AllowAnonymous()),
// nunca o padrão implícito. ICurrentIdentity.GetRequired() continua existindo como segunda barreira
// dentro dos casos de uso (defesa em profundidade), lendo a MESMA identidade já estabelecida por
// HttpContext.User — nunca uma fonte paralela/conflitante.
const string DevelopmentAuthScheme = BlueprintOS.Api.Identity.DevelopmentHeaderAuthenticationDefaults.Scheme;
const string SessionAuthScheme = SessionCookieAuthenticationDefaults.Scheme;
const string BootstrapAuthScheme = BootstrapSessionAuthenticationDefaults.Scheme;

// BootstrapSession é registrado em TODOS os ambientes, como esquema ADICIONAL — nunca altera o esquema
// default do host (DevelopmentHeader/SessionCookie continuam exatamente como antes) e nunca é usado pela
// FallbackPolicy global (security-design-auth-o1.4.md §20.7/§20.13; Work Order O1.4.3, seção 8.1).
if (builder.Environment.IsDevelopment())
{
    // O fluxo real de login OTP (O1.4.2) emite o cookie mc_sid independentemente do ambiente — mas em
    // Development o esquema default era exclusivamente DevelopmentHeader, que nunca olha para o cookie.
    // Resultado: uma sessão real criada por /auth/otp/verify nunca autenticava em /auth/me localmente
    // (fail-closed correto, porém bloqueava o teste do fluxo normal em Development). O PolicyScheme abaixo
    // decide por requisição, SEM enfraquecer nenhuma checagem existente: com cookie mc_sid presente, usa a
    // mesma validação de sessão de produção (SessionCookieAuthenticationHandler); sem o cookie, mantém o
    // comportamento anterior via header (DevelopmentHeaderAuthenticationHandler), preservando os testes e
    // fluxos existentes que dependem de X-Development-User-Id.
    // ICurrentIdentity NÃO reparsa o header nem o cookie — ambos os authentication handlers acima já
    // publicam exatamente as mesmas claims (NameIdentifier + Role) em HttpContext.User antes de qualquer
    // caso de uso rodar. SessionCurrentIdentity (mesma classe usada fora de Development) só lê essa
    // identidade já resolvida, então a prioridade sessão-real-sobre-header é garantida pelo
    // ForwardDefaultSelector abaixo, nunca por uma segunda fonte de verdade aqui: com mc_sid presente,
    // somente SessionCookieAuthenticationHandler roda (o header é ignorado, mesmo se enviado); sem o
    // cookie, somente DevelopmentHeaderAuthenticationHandler roda. Nenhum dos dois nunca autentica os
    // dois ao mesmo tempo, então não há ambiguidade para esta classe resolver.
    const string DevelopmentDefaultScheme = "DevelopmentOrSessionCookie";
    builder.Services.AddScoped<ICurrentIdentity, SessionCurrentIdentity>();
    builder.Services.AddAuthentication(DevelopmentDefaultScheme)
        .AddPolicyScheme(DevelopmentDefaultScheme, DevelopmentDefaultScheme, policyOptions =>
        {
            policyOptions.ForwardDefaultSelector = context =>
                context.Request.Cookies.ContainsKey(AuthCookie.Name)
                    ? SessionAuthScheme
                    : DevelopmentAuthScheme;
        })
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevelopmentHeaderAuthenticationHandler>(DevelopmentAuthScheme, null)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(SessionAuthScheme, null)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, BootstrapSessionAuthenticationHandler>(BootstrapAuthScheme, null);
}
else
{
    builder.Services.AddScoped<ICurrentIdentity, SessionCurrentIdentity>();
    builder.Services.AddAuthentication(SessionAuthScheme)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(SessionAuthScheme, null)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, BootstrapSessionAuthenticationHandler>(BootstrapAuthScheme, null);
}

builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, BootstrapNaoConcluidoAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Política própria do Bootstrap (Work Order O1.4.3, seção 8.1) — nunca reaproveita a FallbackPolicy
    // global (que aceitaria SessionCookie/DevelopmentHeader). Exige exclusivamente o esquema
    // BootstrapSession autenticado E BootstrapEstado.Concluido == false (checagem adicional via
    // IAuthorizationRequirement customizado, não apenas presença de claim).
    options.AddPolicy(BootstrapAuthorizationPolicies.BootstrapAuthenticated, policy => policy
        .AddAuthenticationSchemes(BootstrapAuthScheme)
        .RequireAuthenticatedUser()
        .AddRequirements(new BootstrapNaoConcluidoRequirement()));
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityAuthCore(builder.Configuration);

// Exatamente uma implementação válida de IOtpEmailSender por ambiente — nunca fallback silencioso
// (security-design-auth-o1.4.md, §17.3/§17.4). Seleção feita aqui, na composição raiz, exclusivamente
// por IHostEnvironment.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<DevelopmentOtpInspectionStore>();
    builder.Services.AddScoped<IOtpEmailSender, DevelopmentOtpEmailSender>();
}
else
{
    builder.Services.AddUnconfiguredCorporateOtpEmailSender(builder.Configuration);
}

builder.Services.AddRateLimiter(RateLimitingPolicies.Configure);
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });
}

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health Endpoint — anônimo por exceção explícita: usado por orquestração/monitoramento, que não
// possui e não deveria precisar de uma sessão autenticada.
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Application = "BlueprintOS",
        Environment = app.Environment.EnvironmentName,
        Version = "1.0.0"
    });
}).AllowAnonymous();

app.MapNegotiationRecommendation();
app.MapFornecedores();
app.MapFornecedorDiscovery();
app.MapFornecedorSync();
app.MapAuth();
app.MapBootstrap();
if (app.Environment.IsDevelopment())
{
    app.MapDevelopmentOtpDiagnostics();
}

app.Run();
return 0;

static async Task<int> RunPublicationEngineAsync(string[] args)
{
    var repoRoot = FindRepoRoot(AppContext.BaseDirectory) ?? Directory.GetCurrentDirectory();
    Directory.SetCurrentDirectory(repoRoot);

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);

#pragma warning disable ASP0000 // ponto de entrada isolado para o CLI de publicação, sem relação com o host web.
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000

    var publicationService = provider.GetRequiredService<IPublicationService>();

    IReadOnlyList<PublishedArtifact> artifacts;
    try
    {
        artifacts = await publicationService.PublishAllAsync();
    }
    catch (InvalidOperationException ex)
    {
        Console.Error.WriteLine($"Publication Engine: configuração inválida — {ex.Message}");
        return 1;
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.Error.WriteLine($"Publication Engine: {ex.Message}");
        return 1;
    }

    Console.WriteLine($"Publication Engine: {artifacts.Count} artefato(s) publicado(s) em dist/.");
    foreach (var artifact in artifacts)
    {
        Console.WriteLine($"  - {artifact.RelativePath}");
    }

    var healthService = provider.GetRequiredService<IDocumentationHealthService>();
    var healthReport = await healthService.AnalyzeAsync(artifacts);
    var healthReportPath = await healthService.WriteReportAsync(healthReport);

    Console.WriteLine(
        $"Documentation Health: {healthReport.HealthyCount} saudável(is), {healthReport.WarningCount} aviso(s), {healthReport.ErrorCount} erro(s). Relatório em {healthReportPath}.");

    return 0;
}

static string? FindRepoRoot(string startDirectory)
{
    var directory = startDirectory;
    while (directory is not null)
    {
        if (Directory.Exists(Path.Combine(directory, ".git")))
        {
            return directory;
        }

        directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
    }

    return null;
}

static async Task<int> RunMigrationsAsync()
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);
#pragma warning disable ASP0000 // Isolated CLI composition root; no ASP.NET host is created.
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
    await DatabaseMigrationService.ApplyPendingMigrationsAsync(provider);
    Console.WriteLine("Connectivity confirmed and pending migrations applied.");
    return 0;
}

static async Task<int> ValidateMaisComprasAsync()
{
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables()
        .Build();

    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);
#pragma warning disable ASP0000 // Isolated CLI composition root; no ASP.NET host is created.
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
    var result = await provider.GetRequiredService<B1ConnectivityValidator>().ValidateMaisComprasAsync();
    WriteConnectivityResult(result);
    return result.IsSuccess ? 0 : 1;
}

static async Task<int> ValidateB1ConnectivityAsync()
{
    var configuration = BuildDatabaseConfiguration();
    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);
#pragma warning disable ASP0000 // Isolated CLI composition root; no ASP.NET host is created.
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
    var validator = provider.GetRequiredService<B1ConnectivityValidator>();
    var maisCompras = await validator.ValidateMaisComprasAsync();
    var erp = await validator.ValidateErpAsync();

    Console.WriteLine($"+Compras ........ {(maisCompras.IsSuccess ? "SUCESSO" : "FALHA")}");
    Console.WriteLine($"ERP SOMA_DESENV . {(erp.IsSuccess ? "SUCESSO" : "FALHA")}");
    WriteConnectivityError(maisCompras);
    WriteConnectivityError(erp);
    return maisCompras.IsSuccess && erp.IsSuccess ? 0 : 1;
}

static async Task<int> ProbeErpSuppliersAsync(string[] args)
{
    var term = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) ? args[1] : "0";
    var configuration = BuildDatabaseConfiguration();
    var services = new ServiceCollection();
    services.AddInfrastructure(configuration);
#pragma warning disable ASP0000
    await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
    var candidates = await provider.GetRequiredService<IErpFornecedorDiscoveryRepository>()
        .DescobrirAsync(new(term, term, term));
    Console.WriteLine($"Candidatos ERP encontrados: {candidates.Count}");
    foreach (var candidate in candidates.Take(20))
    {
        var id = string.IsNullOrWhiteSpace(candidate.CodigoFornecedor) ? "[sem-id]" : candidate.CodigoFornecedor;
        var maskedCnpj = string.IsNullOrWhiteSpace(candidate.Cnpj) ? "[sem-documento]" : $"***{candidate.Cnpj[^Math.Min(4, candidate.Cnpj.Length)..]}";
        Console.WriteLine($"ERP_ID={id}; CNPJ={maskedCnpj}; Nome=[SANITIZADO]");
    }
    return 0;
}

static async Task<int> ProbeErpSupplierIntegrityAsync()
{
    var connectionString = BuildDatabaseConfiguration().GetConnectionString("ErpConnection");
    if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("ERP não configurado.");
    var builder = new SqlConnectionStringBuilder(connectionString);
    if (!string.Equals(builder.InitialCatalog, "SOMA_DESENV", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("O probe exige SOMA_DESENV.");
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using var metadata = connection.CreateCommand();
    metadata.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('FORNECEDORES','CADASTRO_CLI_FOR') AND (COLUMN_NAME LIKE '%DATA%' OR COLUMN_NAME LIKE '%ALTER%' OR COLUMN_NAME LIKE '%UPDATE%' OR COLUMN_NAME LIKE '%TRANSFER%') ORDER BY TABLE_NAME, ORDINAL_POSITION";
    await using var reader = await metadata.ExecuteReaderAsync();
    Console.WriteLine("Timestamp candidates:");
    while (await reader.ReadAsync()) Console.WriteLine($"{reader.GetString(0)}.{reader.GetString(1)}.{reader.GetString(2)} ({reader.GetString(3)})");
    await reader.DisposeAsync();
    await using var canonicalColumns = connection.CreateCommand();
    canonicalColumns.CommandText = "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME IN ('FORNECEDORES','CADASTRO_CLI_FOR') AND COLUMN_NAME IN ('NOME_CLIFOR','RAZAO_SOCIAL','CGC_CPF','PJ_PF','RG_IE','CEP','ENDERECO','NUMERO','COMPLEMENTO','BAIRRO','CIDADE','UF','COD_MUNICIPIO_IBGE','PAIS','DDD1','TELEFONE1','EMAIL','EMAIL_NFE','BANCO','CC_AGENCIA','CC_CONTA','CONDICAO_PGTO','TIPO_FORNECEDOR','SUBTIPO_FORNECEDOR','CTB_CONTA_CONTABIL','FORNECE_MATERIAIS','FORNECE_MAT_CONSUMO','FORNECE_OUTROS','FORNECE_PROD_ACAB','BENEFICIADOR','LICENCIADO','INDICADOR_FISCAL_TERCEIRO','ATIVIDADE_SIMPLES_NACIONAL') ORDER BY TABLE_NAME, ORDINAL_POSITION";
    await using var canonicalReader = await canonicalColumns.ExecuteReaderAsync();
    Console.WriteLine("Canonical candidate columns:");
    while (await canonicalReader.ReadAsync()) Console.WriteLine($"{canonicalReader[0]}.{canonicalReader[1]} ({canonicalReader[2]})");
    await canonicalReader.DisposeAsync();
    await using var relatedColumns = connection.CreateCommand();
    relatedColumns.CommandText = "SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME IN ('FORNECEDORES','CADASTRO_CLI_FOR') AND (COLUMN_NAME LIKE '%NOME%' OR COLUMN_NAME LIKE '%TIPO%' OR COLUMN_NAME LIKE '%CLASIF%' OR COLUMN_NAME LIKE '%FONECE%' OR COLUMN_NAME LIKE '%FORNECE%' OR COLUMN_NAME LIKE '%LICEN%' OR COLUMN_NAME LIKE '%BENEF%') ORDER BY TABLE_NAME, ORDINAL_POSITION";
    await using var relatedReader = await relatedColumns.ExecuteReaderAsync();
    Console.WriteLine("Related candidate columns:");
    while (await relatedReader.ReadAsync()) Console.WriteLine($"{relatedReader[0]}.{relatedReader[1]} ({relatedReader[2]})");
    await relatedReader.DisposeAsync();
    await using var canonicalSample = connection.CreateCommand();
    canonicalSample.CommandText = "SELECT TOP (1) f.COD_FORNECEDOR, f.FORNECEDOR, f.TIPO, f.SUBTIPO_FORNECEDOR, f.CONDICAO_PGTO, f.FORNECE_MATERIAIS, f.FORNECE_MAT_CONSUMO, f.FORNECE_OUTROS, f.FORNECE_PROD_ACAB, f.BENEFICIADOR, f.LICENCIADO, c.NOME_CLIFOR, c.RAZAO_SOCIAL, c.PJ_PF, c.RG_IE, c.CEP, c.ENDERECO, c.NUMERO, c.COMPLEMENTO, c.BAIRRO, c.CIDADE, c.UF, c.PAIS, c.DDD1, c.TELEFONE1, c.EMAIL, c.EMAIL_NFE, c.BANCO, c.CC_AGENCIA, c.CC_CONTA, c.CTB_CONTA_CONTABIL, c.INDICADOR_FISCAL_TERCEIRO, c.ATIVIDADE_SIMPLES_NACIONAL, c.TIPO_TRIBUTACAO, c.TIPO_RELACAO_COMERCIAL, c.ID_CLASIF_CLIFOR FROM dbo.FORNECEDORES f LEFT JOIN dbo.CADASTRO_CLI_FOR c ON c.COD_CLIFOR = f.CLIFOR WHERE f.COD_FORNECEDOR = '315502'";
    await using var canonicalSampleReader = await canonicalSample.ExecuteReaderAsync();
    Console.WriteLine("Canonical sample 315502:");
    while (await canonicalSampleReader.ReadAsync()) for (var i = 0; i < canonicalSampleReader.FieldCount; i++) Console.WriteLine($"{canonicalSampleReader.GetName(i)}={canonicalSampleReader[i]}");
    await canonicalSampleReader.DisposeAsync();
    await using var invalid = connection.CreateCommand();
    invalid.CommandText = "SELECT TOP (10) COD_FORNECEDOR, CLIFOR, INATIVO, FORNECEDOR FROM dbo.FORNECEDORES WHERE COD_FORNECEDOR = @id OR CLIFOR = @id";
    invalid.Parameters.Add(new SqlParameter("@id", "00000*"));
    await using var invalidReader = await invalid.ExecuteReaderAsync();
    Console.WriteLine("Invalid-key records:");
    while (await invalidReader.ReadAsync()) Console.WriteLine($"COD_FORNECEDOR={invalidReader[0]}; CLIFOR={invalidReader[1]}; INATIVO={invalidReader[2]}; NOME=[SANITIZADO]");
    await invalidReader.DisposeAsync();
    await using var timestamps = connection.CreateCommand();
    timestamps.CommandText = "SELECT f.COD_FORNECEDOR, f.DATA_PARA_TRANSFERENCIA AS FORNECEDOR_TIMESTAMP, c.DATA_PARA_TRANSFERENCIA AS CADASTRO_TIMESTAMP FROM dbo.FORNECEDORES f LEFT JOIN dbo.CADASTRO_CLI_FOR c ON c.COD_CLIFOR = f.CLIFOR WHERE f.COD_FORNECEDOR IN ('900001', '00000*')";
    await using var timestampReader = await timestamps.ExecuteReaderAsync();
    Console.WriteLine("Timestamp samples:");
    while (await timestampReader.ReadAsync()) Console.WriteLine($"COD_FORNECEDOR={timestampReader[0]}; FORNECEDORES.DATA_PARA_TRANSFERENCIA={timestampReader[1]}; CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA={timestampReader[2]}");
    await timestampReader.DisposeAsync();
    await using var fictitious = connection.CreateCommand();
    fictitious.CommandText = "SELECT f.COD_FORNECEDOR, f.CLIFOR, f.CGC_CPF, f.INATIVO, c.COD_CLIFOR, c.CGC_CPF FROM dbo.FORNECEDORES f LEFT JOIN dbo.CADASTRO_CLI_FOR c ON c.COD_CLIFOR = f.CLIFOR WHERE f.CGC_CPF IN ('52345678000195', '62345678000195', '72345678000195', '82345678000195', '92345678000195') OR f.COD_FORNECEDOR IN ('315501', '315502', '315503', '315504') ORDER BY f.COD_FORNECEDOR";
    await using var fictitiousReader = await fictitious.ExecuteReaderAsync();
    Console.WriteLine("Fictitious supplier keys:");
    while (await fictitiousReader.ReadAsync()) Console.WriteLine($"COD_FORNECEDOR={fictitiousReader[0]}; CLIFOR={fictitiousReader[1]}; FORNECEDORES.CGC_CPF={fictitiousReader[2]}; INATIVO={fictitiousReader[3]}; CADASTRO_CLI_FOR.COD_CLIFOR={fictitiousReader[4]}; CADASTRO_CLI_FOR.CGC_CPF={fictitiousReader[5]}");
    await fictitiousReader.DisposeAsync();
    await using var confirmation = connection.CreateCommand();
    confirmation.CommandText = "SELECT TOP (1) f.COD_FORNECEDOR, f.FORNECEDOR, f.CGC_CPF, f.INATIVO, f.DATA_PARA_TRANSFERENCIA, c.COD_CLIFOR, c.NOME_CLIFOR, c.CGC_CPF, c.DATA_PARA_TRANSFERENCIA FROM dbo.FORNECEDORES f LEFT JOIN dbo.CADASTRO_CLI_FOR c ON c.COD_CLIFOR = f.CLIFOR WHERE f.COD_FORNECEDOR = @id";
    confirmation.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@id", "315501"));
    await using var confirmationReader = await confirmation.ExecuteReaderAsync();
    while (await confirmationReader.ReadAsync()) Console.WriteLine($"Confirmation sample: FORNECEDORES.COD_FORNECEDOR={confirmationReader[0]}; FORNECEDORES.FORNECEDOR={confirmationReader[1]}; FORNECEDORES.CGC_CPF={confirmationReader[2]}; INATIVO={confirmationReader[3]}; CADASTRO_CLI_FOR.COD_CLIFOR={confirmationReader[5]}; CADASTRO_CLI_FOR.NOME_CLIFOR={confirmationReader[6]}");
    await confirmationReader.DisposeAsync();
    await using var procedure = connection.CreateCommand();
    procedure.CommandText = "SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.LX_AZZ_GERAR_FORNECEDOR_LINX'))";
    var definition = Convert.ToString(await procedure.ExecuteScalarAsync()) ?? string.Empty;
    Console.WriteLine("Reference procedure markers:");
    foreach (var line in definition.Split('\n').Where(x => x.Contains("DATA_PARA_TRANSFERENCIA", StringComparison.OrdinalIgnoreCase) || x.Contains("LX_SEQUENCIAL", StringComparison.OrdinalIgnoreCase) || x.Contains("CLIFOR", StringComparison.OrdinalIgnoreCase)).Take(30)) Console.WriteLine(line.Trim());
    return 0;
}

static IConfiguration BuildDatabaseConfiguration() => new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

static void WriteConnectivityResult(DatabaseConnectivityResult result)
{
    Console.WriteLine($"{result.Label} ........ {(result.IsSuccess ? "SUCESSO" : "FALHA")}");
    WriteConnectivityError(result);
}

static void WriteConnectivityError(DatabaseConnectivityResult result)
{
    if (result.IsSuccess || result.Exception is null) return;
    Console.WriteLine($"  Exceção: {result.Exception.GetType().FullName}");
    Console.WriteLine($"  Mensagem: {SanitizeConnectivityMessage(result.Exception.Message)}");
    Console.WriteLine($"  Servidor: {result.Server ?? "não disponível"}");
    Console.WriteLine($"  Banco: {result.Database ?? "não disponível"}");
}

static string SanitizeConnectivityMessage(string message)
{
    var sanitized = Regex.Replace(message, "(?i)(login failed for user\\s*)'[^']*'", "$1'[REDACTED]'");
    return Regex.Replace(sanitized, "(?i)(user id|uid|password|pwd)\\s*=\\s*[^;\\r\\n]*", "$1=[REDACTED]");
}
