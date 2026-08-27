using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Api.Administration;
using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Api.Identity;
using BlueprintOS.Api.Knowledge;
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

if (args.Length > 0 && args[0] == "investigate-linx-prog-op-ped")
{
    return await InvestigateLinxProgOpPedAsync(args);
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

// Enums sao serializados como string (nome do valor), nunca como o inteiro
// subjacente — contrato HTTP explicito e deterministico para o frontend
// (ex.: SituacaoCadastralCnpj, TipoErroConsultaCnpj, StatusConsultaCnpj).
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

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

// O1.5 — RBAC Real. Handler idiomático de Authorization que decide sobre as claims de permissão já
// publicadas pelo authentication handler (nenhum I/O por decisão de autorização).
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, BlueprintOS.Api.Authorization.PermissaoAuthorizationHandler>();

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

    // O1.5 — uma policy por permissão do catálogo, geradas por iteração sobre PermissaoCatalogo (fonte
    // central única). Nenhum código de permissão é escrito literalmente aqui nem nos endpoints.
    options.AddRbacPolicies();
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityAuthCore(builder.Configuration);

// O1.11 — Data Protection para cifragem de segredos de configuração técnica (IdentityProvider/
// ConfiguracaoErp), via ISegredoProtector (registrado em AddIdentityAuthCore). Nunca em texto claro.
builder.Services.AddDataProtection();

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
app.MapPerfis();
app.MapUsuarios();
app.MapFiliais();
app.MapCentrosCusto();
app.MapUnidadesAlocacao();
app.MapMe();
app.MapUnidadesNegocio();
app.MapIdentityProviders();
app.MapConfiguracaoErp();
app.MapConfiguracaoNotificacao();
app.MapParametros();
app.MapFeatureFlags();
app.MapRegrasWorkflow();
app.MapAlcadasAprovacao();
app.MapRegrasOrcamentarias();
app.MapMonitoramentoOperacional();
app.MapLinxKnowledge();
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
    var erpDev = await validator.ValidateErpAsync(LinxEnvironment.Development);
    var erpProd = await validator.ValidateErpAsync(LinxEnvironment.Production);

    WriteConnectivityResult(maisCompras);
    WriteConnectivityResult(erpDev);
    WriteConnectivityResult(erpProd);
    // Production não é exigida para o comando genérico ter sucesso: um dev pode não ter (nem precisa
    // ter) a credencial de Production configurada localmente para trabalhar em Development.
    return maisCompras.IsSuccess && erpDev.IsSuccess ? 0 : 1;
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

/// <summary>Investigação read-only do caso PROG/OP/PED (docs/audits/AgentLearningV1-LinxProgOpPed.md),
/// contra o profile Development (SOMA_DESENV) ou Production (SOMA) — selecionável via <c>--env=production</c>
/// em qualquer posição dos argumentos (default: Development, para não mudar o comportamento de invocações
/// anteriores). Emite apenas SELECT/metadata (INFORMATION_SCHEMA, sys.*, OBJECT_DEFINITION) — nenhum
/// INSERT/UPDATE/DELETE/MERGE/DDL/EXEC de procedure mutável. Modos: "schema" (colunas/PK/índices das 5
/// tabelas + definição das 4 procedures + busca por tabelas/colunas de grade/tamanho), "grade" (mecanismo
/// PRODUTOS/PRODUTOS_TAMANHOS para os produtos da planilha) e "crossref" (cruza produto/cor/programação da
/// planilha, lida de um JSON local fora do Git, contra PRODUCAO_PROG_PROD/PRODUCAO_ORDEM(_COR)/COMPRAS(_PRODUTO)).</summary>
static async Task<int> InvestigateLinxProgOpPedAsync(string[] args)
{
    var positional = args.Where(a => !a.StartsWith("--env=", StringComparison.Ordinal)).ToArray();
    var envArg = args.FirstOrDefault(a => a.StartsWith("--env=", StringComparison.Ordinal))?["--env=".Length..];
    var environment = string.Equals(envArg, "production", StringComparison.OrdinalIgnoreCase)
        ? BlueprintOS.Infrastructure.Persistence.LinxEnvironment.Production
        : BlueprintOS.Infrastructure.Persistence.LinxEnvironment.Development;
    var profile = BlueprintOS.Infrastructure.Persistence.LinxConnectionProfiles.Resolve(environment);

    var mode = positional.Length > 1 ? positional[1] : "schema";
    var configuration = BuildDatabaseConfiguration();
    var connectionString = BlueprintOS.Infrastructure.Persistence.LinxConnectionStringResolver.Resolve(configuration, profile);

    Console.WriteLine($"[investigate-linx-prog-op-ped] profile={profile.Label} server={profile.ExpectedServer} database={profile.ExpectedDatabase}");

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    args = positional;

    if (mode == "schema")
    {
        string[] tabelas = ["PRODUCAO_PROG_PROD", "PRODUCAO_ORDEM", "PRODUCAO_ORDEM_COR", "COMPRAS", "COMPRAS_PRODUTO"];
        foreach (var tabela in tabelas)
        {
            Console.WriteLine($"===== TABELA {tabela} =====");
            await using (var cols = connection.CreateCommand())
            {
                cols.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, ORDINAL_POSITION FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t ORDER BY ORDINAL_POSITION";
                cols.Parameters.Add(new SqlParameter("@t", tabela));
                await using var reader = await cols.ExecuteReaderAsync();
                Console.WriteLine("-- Colunas --");
                while (await reader.ReadAsync())
                    Console.WriteLine($"{reader[3]}\t{reader[0]}\t{reader[1]}\tNULL={reader[2]}");
            }

            await using (var pk = connection.CreateCommand())
            {
                pk.CommandText = @"SELECT kcu.COLUMN_NAME, kcu.ORDINAL_POSITION
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                    WHERE tc.TABLE_NAME = @t AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ORDER BY kcu.ORDINAL_POSITION";
                pk.Parameters.Add(new SqlParameter("@t", tabela));
                await using var reader = await pk.ExecuteReaderAsync();
                Console.WriteLine("-- Primary Key --");
                while (await reader.ReadAsync())
                    Console.WriteLine($"{reader[1]}\t{reader[0]}");
            }

            await using (var idx = connection.CreateCommand())
            {
                idx.CommandText = @"SELECT i.name AS index_name, i.is_unique, i.type_desc, c.name AS column_name, ic.key_ordinal
                    FROM sys.indexes i
                    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    WHERE i.object_id = OBJECT_ID(@t) AND i.name IS NOT NULL
                    ORDER BY i.name, ic.key_ordinal";
                idx.Parameters.Add(new SqlParameter("@t", $"dbo.{tabela}"));
                await using var reader = await idx.ExecuteReaderAsync();
                Console.WriteLine("-- Indexes (uma linha por coluna) --");
                while (await reader.ReadAsync())
                    Console.WriteLine($"{reader[0]}\tunique={reader[1]}\t{reader[2]}\tcol#{reader[4]}={reader[3]}");
            }
        }

        string[] procedures = ["LX_ANM_GERA_OS_ALTERACAO_PCP", "LX_ANM_AJUSTA_PROGRAMACAO_PROD", "LX_MOVIMENTA_COMPRAS_PA", "LX_RECALCULO_RESERVA_MATERIAIS"];
        foreach (var proc in procedures)
        {
            Console.WriteLine($"===== PROCEDURE {proc} =====");
            await using var exists = connection.CreateCommand();
            exists.CommandText = "SELECT OBJECT_ID(@p, 'P')";
            exists.Parameters.Add(new SqlParameter("@p", $"dbo.{proc}"));
            var objectId = await exists.ExecuteScalarAsync();
            if (objectId is null or DBNull)
            {
                Console.WriteLine("NAO ENCONTRADA em dbo.");
                continue;
            }

            await using (var pars = connection.CreateCommand())
            {
                pars.CommandText = @"SELECT p.name, TYPE_NAME(p.user_type_id), p.is_output
                    FROM sys.parameters p WHERE p.object_id = OBJECT_ID(@p) ORDER BY p.parameter_id";
                pars.Parameters.Add(new SqlParameter("@p", $"dbo.{proc}"));
                await using var reader = await pars.ExecuteReaderAsync();
                Console.WriteLine("-- Parametros --");
                while (await reader.ReadAsync())
                    Console.WriteLine($"{reader[0]}\t{reader[1]}\tOUTPUT={reader[2]}");
            }

            await using var def = connection.CreateCommand();
            def.CommandText = "SELECT OBJECT_DEFINITION(OBJECT_ID(@p))";
            def.Parameters.Add(new SqlParameter("@p", $"dbo.{proc}"));
            var definition = Convert.ToString(await def.ExecuteScalarAsync()) ?? "(definicao nao acessivel/encriptada)";
            Console.WriteLine("-- Definicao --");
            Console.WriteLine(definition);
        }

        Console.WriteLine("===== BUSCA POR TABELAS/COLUNAS DE GRADE/TAMANHO =====");
        await using (var grade = connection.CreateCommand())
        {
            grade.CommandText = @"SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
                WHERE COLUMN_NAME LIKE '%GRADE%' OR COLUMN_NAME LIKE '%TAMANHO%' OR COLUMN_NAME LIKE '%GRD%'
                   OR (COLUMN_NAME LIKE 'TAM[_]%' ESCAPE '\')
                ORDER BY TABLE_NAME, ORDINAL_POSITION";
            await using var reader = await grade.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                Console.WriteLine($"{reader[0]}.{reader[1]} ({reader[2]})");
        }

        return 0;
    }

    if (mode == "crossref")
    {
        var jsonPath = args.Length > 2 ? args[2] : "downloads/showcase_produtos/planilha_rows.json";
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"Arquivo nao encontrado: {jsonPath}");
            return 1;
        }

        var json = await File.ReadAllTextAsync(jsonPath);
        var rows = System.Text.Json.JsonDocument.Parse(json).RootElement;

        foreach (var row in rows.EnumerateArray())
        {
            var produto = row.GetProperty("produto").GetString();
            var programacao = row.GetProperty("programacao").GetString();
            var cor = row.GetProperty("cor").GetString();
            var po = row.GetProperty("po").GetInt64().ToString();

            await using var progCmd = connection.CreateCommand();
            progCmd.CommandText = "SELECT COUNT(*) FROM dbo.PRODUCAO_PROG_PROD WHERE PROGRAMACAO = @prog AND PRODUTO = @prod AND COR_PRODUTO = @cor";
            progCmd.Parameters.Add(new SqlParameter("@prog", programacao));
            progCmd.Parameters.Add(new SqlParameter("@prod", produto));
            progCmd.Parameters.Add(new SqlParameter("@cor", cor));
            var progMatch = Convert.ToInt32(await progCmd.ExecuteScalarAsync());

            await using var opCmd = connection.CreateCommand();
            opCmd.CommandText = @"SELECT COUNT(*) FROM dbo.PRODUCAO_ORDEM A
                JOIN dbo.PRODUCAO_ORDEM_COR B ON A.ORDEM_PRODUCAO = B.ORDEM_PRODUCAO AND A.PRODUTO = B.PRODUTO
                WHERE A.PROGRAMACAO = @prog AND A.PRODUTO = @prod AND B.COR_PRODUTO = @cor";
            opCmd.Parameters.Add(new SqlParameter("@prog", programacao));
            opCmd.Parameters.Add(new SqlParameter("@prod", produto));
            opCmd.Parameters.Add(new SqlParameter("@cor", cor));
            var opMatch = Convert.ToInt32(await opCmd.ExecuteScalarAsync());

            await using var pedCmd = connection.CreateCommand();
            pedCmd.CommandText = @"SELECT COUNT(*) FROM dbo.COMPRAS A
                JOIN dbo.COMPRAS_PRODUTO B ON A.PEDIDO = B.PEDIDO
                WHERE A.PROGRAMACAO = @prog AND B.PRODUTO = @prod AND B.COR_PRODUTO = @cor";
            pedCmd.Parameters.Add(new SqlParameter("@prog", programacao));
            pedCmd.Parameters.Add(new SqlParameter("@prod", produto));
            pedCmd.Parameters.Add(new SqlParameter("@cor", cor));
            var pedMatch = Convert.ToInt32(await pedCmd.ExecuteScalarAsync());

            await using var pedByPoCmd = connection.CreateCommand();
            pedByPoCmd.CommandText = "SELECT COUNT(*) FROM dbo.COMPRAS_PRODUTO WHERE PEDIDO = @po AND PRODUTO = @prod AND COR_PRODUTO = @cor";
            pedByPoCmd.Parameters.Add(new SqlParameter("@po", po));
            pedByPoCmd.Parameters.Add(new SqlParameter("@prod", produto));
            pedByPoCmd.Parameters.Add(new SqlParameter("@cor", cor));
            var pedByPoMatch = Convert.ToInt32(await pedByPoCmd.ExecuteScalarAsync());

            Console.WriteLine($"PRODUTO={produto}; PROGRAMACAO={programacao}; COR={cor}; PO={po} => PRODUCAO_PROG_PROD={progMatch}; OP={opMatch}; PED(by programacao)={pedMatch}; PED(by PO)={pedByPoMatch}");
        }

        return 0;
    }

    if (mode == "grade")
    {
        string[] views = ["ANM_VIEW_GRADE_TAMANHO", "ANM_VIEW_GRADE_TAMANHO_INTERNACIONAL"];
        foreach (var view in views)
        {
            Console.WriteLine($"===== VIEW/OBJETO {view} =====");
            await using var def = connection.CreateCommand();
            def.CommandText = "SELECT OBJECT_DEFINITION(OBJECT_ID(@p))";
            def.Parameters.Add(new SqlParameter("@p", $"dbo.{view}"));
            var definition = Convert.ToString(await def.ExecuteScalarAsync()) ?? "(nao encontrada/nao acessivel)";
            Console.WriteLine(definition);
        }

        Console.WriteLine("===== TABELA PRODUTO (colunas relacionadas a grade) =====");
        await using (var prodCols = connection.CreateCommand())
        {
            prodCols.CommandText = @"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'PRODUTO' AND (COLUMN_NAME LIKE '%GRADE%' OR COLUMN_NAME LIKE '%TAMANHO%')
                ORDER BY ORDINAL_POSITION";
            await using var reader = await prodCols.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                Console.WriteLine($"{reader[0]} ({reader[1]})");
        }

        var jsonPath = args.Length > 2 ? args[2] : "downloads/showcase_produtos/planilha_rows.json";
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"Arquivo nao encontrado: {jsonPath}");
            return 1;
        }
        var json = await File.ReadAllTextAsync(jsonPath);
        var rows = System.Text.Json.JsonDocument.Parse(json).RootElement;
        var produtos = rows.EnumerateArray().Select(r => r.GetProperty("produto").GetString()).Distinct().ToList();

        string[] candidateTables = ["PRODUTOS", "PRODUTOS_TAMANHOS"];
        foreach (var tabela in candidateTables)
        {
            Console.WriteLine($"===== TABELA {tabela} (colunas) =====");
            await using var cols = connection.CreateCommand();
            cols.CommandText = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t ORDER BY ORDINAL_POSITION";
            cols.Parameters.Add(new SqlParameter("@t", tabela));
            await using var reader = await cols.ExecuteReaderAsync();
            while (await reader.ReadAsync()) Console.WriteLine($"{reader[0]} ({reader[1]})");
        }

        Console.WriteLine("===== PRODUTOS.GRADE + PRODUTOS_TAMANHOS PARA OS PRODUTOS DA PLANILHA =====");
        foreach (var produto in produtos)
        {
            string? grade = null;
            await using (var prodRow = connection.CreateCommand())
            {
                prodRow.CommandText = "SELECT GRADE, TAMANHO_BASE FROM dbo.PRODUTOS WHERE PRODUTO = @p";
                prodRow.Parameters.Add(new SqlParameter("@p", produto));
                await using var reader = await prodRow.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    grade = reader.GetString(0);
                    Console.WriteLine($"PRODUTO={produto}; GRADE={grade}; TAMANHO_BASE={reader[1]}");
                }
                else
                {
                    Console.WriteLine($"PRODUTO={produto}; NAO ENCONTRADO em PRODUTOS");
                    continue;
                }
            }

            await using var tamRow = connection.CreateCommand();
            tamRow.CommandText = "SELECT NUMERO_TAMANHOS, TAMANHO_1, TAMANHO_2, TAMANHO_3, TAMANHO_4, TAMANHO_5, TAMANHO_6, TAMANHO_7, TAMANHO_8 FROM dbo.PRODUTOS_TAMANHOS WHERE GRADE = @g";
            tamRow.Parameters.Add(new SqlParameter("@g", grade));
            await using (var reader = await tamRow.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var pairs = new List<string>();
                    for (var i = 0; i < reader.FieldCount; i++)
                        pairs.Add($"{reader.GetName(i)}={reader.GetValue(i)}");
                    Console.WriteLine($"  PRODUTOS_TAMANHOS[GRADE={grade}]: {string.Join("; ", pairs)}");
                }
                else
                {
                    Console.WriteLine($"  PRODUTOS_TAMANHOS[GRADE={grade}]: NAO ENCONTRADO");
                }
            }
        }

        return 0;
    }

    if (mode == "grade-detail")
    {
        Console.WriteLine("===== PRODUTOS_TAMANHOS: QUEBRA_1..5 e detalhe completo para GRADE='36-44' =====");
        await using var detail = connection.CreateCommand();
        detail.CommandText = "SELECT GRADE, NUMERO_TAMANHOS, NUMERO_QUEBRAS, QUEBRA_1, QUEBRA_2, QUEBRA_3, QUEBRA_4, QUEBRA_5, TAMANHOS_DIGITADOS, GRADE_BASE, GRADE_CODIGO, INATIVO FROM dbo.PRODUTOS_TAMANHOS WHERE GRADE = '36-44'";
        await using (var reader = await detail.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var pairs = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++) pairs.Add($"{reader.GetName(i)}={reader.GetValue(i)}");
                Console.WriteLine(string.Join("; ", pairs));
            }
        }

        Console.WriteLine("===== Grades distintas cadastradas que contenham '34' em algum TAMANHO_1..8 =====");
        await using var scan = connection.CreateCommand();
        scan.CommandText = @"SELECT DISTINCT GRADE, TAMANHO_1, TAMANHO_2, TAMANHO_3, TAMANHO_4, TAMANHO_5, TAMANHO_6, TAMANHO_7, TAMANHO_8
            FROM dbo.PRODUTOS_TAMANHOS
            WHERE '34' IN (TAMANHO_1, TAMANHO_2, TAMANHO_3, TAMANHO_4, TAMANHO_5, TAMANHO_6, TAMANHO_7, TAMANHO_8)";
        await using (var reader = await scan.ExecuteReaderAsync())
        {
            var any = false;
            while (await reader.ReadAsync())
            {
                any = true;
                var pairs = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++) pairs.Add($"{reader.GetName(i)}={reader.GetValue(i)}");
                Console.WriteLine(string.Join("; ", pairs));
            }
            if (!any) Console.WriteLine("(nenhuma grade cadastrada contem o tamanho '34' em TAMANHO_1..8)");
        }

        Console.WriteLine("===== PO 1741979 (produto 15.29765, cores 09204 e 5465) em COMPRAS/COMPRAS_PRODUTO =====");
        await using var poCmd = connection.CreateCommand();
        poCmd.CommandText = @"SELECT A.PEDIDO, A.PROGRAMACAO, A.STATUS_COMPRA, B.PRODUTO, B.COR_PRODUTO, B.ENTREGA, B.QTDE_ORIGINAL, B.QTDE_ENTREGUE, B.QTDE_ENTREGAR,
                B.CO1, B.CO2, B.CO3, B.CO4, B.CO5
            FROM dbo.COMPRAS A LEFT JOIN dbo.COMPRAS_PRODUTO B ON A.PEDIDO = B.PEDIDO
            WHERE A.PEDIDO = '1741979' OR B.PRODUTO = '15.29765'";
        await using (var reader = await poCmd.ExecuteReaderAsync())
        {
            var any = false;
            while (await reader.ReadAsync())
            {
                any = true;
                var pairs = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++) pairs.Add($"{reader.GetName(i)}={reader.GetValue(i)}");
                Console.WriteLine(string.Join("; ", pairs));
            }
            if (!any) Console.WriteLine("(nenhum registro encontrado para PEDIDO=1741979 nem PRODUTO=15.29765 em COMPRAS/COMPRAS_PRODUTO)");
        }

        return 0;
    }

    if (mode == "delta")
    {
        var jsonPath = positional.Length > 2 ? positional[2] : "downloads/showcase_produtos/planilha_rows.json";
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"Arquivo nao encontrado: {jsonPath}");
            return 1;
        }
        var json = await File.ReadAllTextAsync(jsonPath);
        var rows = System.Text.Json.JsonDocument.Parse(json).RootElement;

        long totalRequested36a44 = 0, totalCurrent36a44 = 0, totalRequested34 = 0;
        int zeroDelta = 0, changeRequired = 0, notFound = 0, blockedSize34 = 0;

        foreach (var row in rows.EnumerateArray())
        {
            var produto = row.GetProperty("produto").GetString();
            var programacao = row.GetProperty("programacao").GetString();
            var cor = row.GetProperty("cor").GetString();
            var po = row.GetProperty("po").GetInt64().ToString();
            int q34 = row.GetProperty("q34").GetInt32(), q36 = row.GetProperty("q36").GetInt32(), q38 = row.GetProperty("q38").GetInt32();
            int q40 = row.GetProperty("q40").GetInt32(), q42 = row.GetProperty("q42").GetInt32(), q44 = row.GetProperty("q44").GetInt32();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT CO1, CO2, CO3, CO4, CO5 FROM dbo.COMPRAS_PRODUTO WHERE PEDIDO = @po AND PRODUTO = @prod AND COR_PRODUTO = @cor";
            cmd.Parameters.Add(new SqlParameter("@po", po));
            cmd.Parameters.Add(new SqlParameter("@prod", produto));
            cmd.Parameters.Add(new SqlParameter("@cor", cor));
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                notFound++;
                Console.WriteLine($"NAO_ENCONTRADO_EM_COMPRAS_PRODUTO; PRODUTO={produto}; PROGRAMACAO={programacao}; COR={cor}; PO={po}");
                continue;
            }

            int co1 = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            int co2 = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            int co3 = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            int co4 = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            int co5 = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

            int d36 = q36 - co1, d38 = q38 - co2, d40 = q40 - co3, d42 = q42 - co4, d44 = q44 - co5;
            var delta = d36 + d38 + d40 + d42 + d44;
            totalRequested36a44 += q36 + q38 + q40 + q42 + q44;
            totalCurrent36a44 += co1 + co2 + co3 + co4 + co5;
            totalRequested34 += q34;
            if (q34 != 0) blockedSize34++;

            var status = delta == 0 && q34 == 0 ? "ZERO_DELTA" : "CHANGE_REQUIRED";
            if (status == "ZERO_DELTA") zeroDelta++; else changeRequired++;

            Console.WriteLine($"{status}; PRODUTO={produto}; PROGRAMACAO={programacao}; COR={cor}; PO={po}; " +
                $"ATUAL(36,38,40,42,44)=({co1},{co2},{co3},{co4},{co5}); SOLICITADO(36,38,40,42,44)=({q36},{q38},{q40},{q42},{q44}); " +
                $"DELTA(36,38,40,42,44)=({d36},{d38},{d40},{d42},{d44}); Q_34_SEM_POSICAO_VALIDA={q34}");
        }

        Console.WriteLine("===== RESUMO =====");
        Console.WriteLine($"ZERO_DELTA={zeroDelta}; CHANGE_REQUIRED={changeRequired}; NAO_ENCONTRADO={notFound}");
        Console.WriteLine($"Linhas com Q_34 nao-zero (sem posicao valida na grade '36-44')={blockedSize34}; total de unidades Q_34={totalRequested34}");
        Console.WriteLine($"Total solicitado (tamanhos 36-44)={totalRequested36a44}; Total atual (tamanhos 36-44)={totalCurrent36a44}; Delta liquido (36-44)={totalRequested36a44 - totalCurrent36a44}");

        return 0;
    }

    Console.WriteLine($"Modo desconhecido: {mode}. Use 'schema', 'grade', 'grade-detail', 'crossref' ou 'delta'.");
    return 1;
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
    Console.WriteLine($"{result.Label} ........ {result.Status.ToString().ToUpperInvariant()}");
    if (result.IsSuccess)
    {
        Console.WriteLine($"  Servidor: {result.Server ?? "não disponível"}");
        Console.WriteLine($"  Banco: {result.Database ?? "não disponível"}");
        Console.WriteLine($"  Identidade efetiva: {result.EffectiveIdentity ?? "não disponível"}");
    }
    if (result.Status == ConnectivityStatus.ConnectivityUnavailable)
    {
        Console.WriteLine("  Não foi possível acessar o servidor após uma tentativa de restabelecimento. Verifique/reconecte a VPN ou a conexão com o servidor e tente novamente.");
    }
    if (result.IsSuccess && result.RecoveredAfterRetry)
    {
        Console.WriteLine("  (conectividade restabelecida automaticamente após 1 nova tentativa)");
    }
    if (result.Status == ConnectivityStatus.EnvironmentMismatch)
    {
        Console.WriteLine("  Bloqueado: a connection string configurada não corresponde ao profile esperado.");
    }
    WriteConnectivityError(result);
}

static void WriteConnectivityError(DatabaseConnectivityResult result)
{
    if (result.IsSuccess || result.Exception is null) return;
    Console.WriteLine($"  Exceção: {result.Exception.GetType().FullName}");
    if (result.Exception is Microsoft.Data.SqlClient.SqlException sqlException)
    {
        Console.WriteLine($"  Código SQL: {sqlException.Number}");
    }
    Console.WriteLine($"  Mensagem: {SanitizeConnectivityMessage(result.Exception.Message)}");
    Console.WriteLine($"  Servidor: {result.Server ?? "não disponível"}");
    Console.WriteLine($"  Banco: {result.Database ?? "não disponível"}");
}

static string SanitizeConnectivityMessage(string message)
{
    var sanitized = Regex.Replace(message, "(?i)(login failed for user\\s*)'[^']*'", "$1'[REDACTED]'");
    return Regex.Replace(sanitized, "(?i)(user id|uid|password|pwd)\\s*=\\s*[^;\\r\\n]*", "$1=[REDACTED]");
}
