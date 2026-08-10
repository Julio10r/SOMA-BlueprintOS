using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.DependencyInjection;

/// <summary>Registra as peças de infraestrutura do módulo de autenticação (Login OTP + sessão server-side)
/// que são independentes de ambiente. A seleção do <see cref="IOtpEmailSender"/> por
/// <c>IHostEnvironment</c> — nunca por appsettings/feature flag (security-design-auth-o1.4.md, §17.4) —
/// é feita na composição raiz (<c>Program.cs</c>), que já tem acesso nativo a <c>IHostEnvironment</c> por
/// ser um projeto Web SDK; este projeto de Infraestrutura permanece sem essa dependência.</summary>
public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityAuthCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.Configure<AuthSessionOptions>(configuration.GetSection(AuthSessionOptions.SectionName));
        services.Configure<OtpRequestThrottleOptions>(configuration.GetSection(OtpRequestThrottleOptions.SectionName));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ICodigoVerificacaoOtpRepository, CodigoVerificacaoOtpRepository>();
        services.AddScoped<ISessaoAutenticacaoRepository, SessaoAutenticacaoRepository>();
        services.AddScoped<IOtpRequestThrottleRepository, OtpRequestThrottleRepository>();

        services.AddScoped<ISolicitarOtpUseCase, SolicitarOtpUseCase>();
        services.AddScoped<IValidarOtpUseCase, ValidarOtpUseCase>();
        services.AddScoped<ILogoutUseCase, LogoutUseCase>();
        services.AddScoped<IObterIdentidadeAtualUseCase, ObterIdentidadeAtualUseCase>();

        AddBootstrapCore(services, configuration);

        return services;
    }

    /// <summary>Fundação Backend do Bootstrap (Work Order O1.4.3, etapa O1.4.3.1). Registra
    /// <see cref="BootstrapSecretOptions"/> (fail-closed fora de Development, mesmo padrão de
    /// <see cref="CorporateOtpEmailSenderOptionsValidator"/>) e <see cref="BootstrapAllowedCandidatesOptions"/>
    /// (fail-closed silencioso — lista ausente/vazia nunca "sem restrição") em todos os ambientes, já que o
    /// Bootstrap não é exclusivo de Development (security-design-auth-o1.4.md §20.13).</summary>
    private static void AddBootstrapCore(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BootstrapSecretOptions>()
            .Bind(configuration.GetSection(BootstrapSecretOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<BootstrapSecretOptions>, BootstrapSecretOptionsValidator>();

        // Bootstrap:AllowedCandidateEmails é um array de strings diretamente nesta chave (não um objeto
        // aninhado) — vinculado explicitamente à propriedade Emails (Work Order O1.4.3, seção 10).
        services.Configure<BootstrapAllowedCandidatesOptions>(o =>
            o.Emails = configuration.GetSection(BootstrapAllowedCandidatesOptions.ConfigurationKey).Get<string[]>() ?? Array.Empty<string>());

        services.AddScoped<IBootstrapEstadoRepository, BootstrapEstadoRepository>();
        services.AddScoped<IBootstrapSessaoRepository, BootstrapSessaoRepository>();

        services.AddScoped<IConsultarBootstrapEstadoUseCase, ConsultarBootstrapEstadoUseCase>();
        services.AddScoped<IIniciarBootstrapUseCase, IniciarBootstrapUseCase>();
        services.AddScoped<IValidarOtpBootstrapUseCase, ValidarOtpBootstrapUseCase>();

        // O1.4.3.2 — Conclusão Transacional e Administrador Sênior (Work Order O1.4.3, seção 13).
        services.AddScoped<IUnidadeNegocioRepository, UnidadeNegocioRepository>();
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IUsuarioPerfilRepository, UsuarioPerfilRepository>();
        services.AddScoped<IConcluirBootstrapUseCase, ConcluirBootstrapUseCase>();
    }

    /// <summary>Registra o provider corporativo (ainda não implementado) fora de Development — fail-closed:
    /// <c>ValidateOnStart()</c> impede a aplicação de subir sem <c>Identity:Otp:Corporate:Provider</c>
    /// configurado (Authentication Infra Readiness Gate, security-design-auth-o1.4.md §17.7).</summary>
    public static IServiceCollection AddUnconfiguredCorporateOtpEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorporateOtpEmailSenderOptions>()
            .Bind(configuration.GetSection(CorporateOtpEmailSenderOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CorporateOtpEmailSenderOptions>, CorporateOtpEmailSenderOptionsValidator>();
        services.AddScoped<IOtpEmailSender, UnconfiguredCorporateOtpEmailSender>();
        return services;
    }
}
