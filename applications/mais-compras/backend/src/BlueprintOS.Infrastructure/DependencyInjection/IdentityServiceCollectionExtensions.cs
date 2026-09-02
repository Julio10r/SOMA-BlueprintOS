using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Infrastructure.Administration;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
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
        AddRbacCore(services);

        return services;
    }

    /// <summary>O1.5 — RBAC Real. Registra a resolução das permissões efetivas (consumida pelo pipeline de
    /// autenticação a cada requisição) e os casos de uso da Gestão de Perfis.
    ///
    /// <c>IPerfilRepository</c>/<c>IUsuarioPerfilRepository</c> já são registrados por
    /// <see cref="AddBootstrapCore"/> (O1.4.3.2) e são reaproveitados aqui — a O1.5 estende os contratos
    /// existentes em vez de criar um segundo caminho de acesso aos mesmos dados.</summary>
    private static void AddRbacCore(IServiceCollection services)
    {
        services.AddScoped<IPermissaoRepository, PermissaoRepository>();
        services.AddScoped<IPermissoesEfetivasResolver, PermissoesEfetivasResolver>();

        services.AddScoped<IListarPerfisUseCase, ListarPerfisUseCase>();
        services.AddScoped<IObterPerfilUseCase, ObterPerfilUseCase>();
        services.AddScoped<ICriarPerfilUseCase, CriarPerfilUseCase>();
        services.AddScoped<IAtualizarPerfilUseCase, AtualizarPerfilUseCase>();
        services.AddScoped<IAlterarStatusPerfilUseCase, AlterarStatusPerfilUseCase>();
        services.AddScoped<IListarCatalogoPermissoesUseCase, ListarCatalogoPermissoesUseCase>();

        // O1.6 — Gestão de Usuários (Backend Real). IUsuarioRepository já é registrado por
        // AddIdentityAuthCore (Login OTP), e IPerfilRepository por AddBootstrapCore — reaproveitados aqui.
        services.AddScoped<IListarUsuariosUseCase, ListarUsuariosUseCase>();
        services.AddScoped<IObterUsuarioUseCase, ObterUsuarioUseCase>();
        services.AddScoped<ICriarUsuarioUseCase, CriarUsuarioUseCase>();
        services.AddScoped<IAtualizarUsuarioUseCase, AtualizarUsuarioUseCase>();
        services.AddScoped<IAlterarStatusUsuarioUseCase, AlterarStatusUsuarioUseCase>();

        // O1.7 — Filiais e Centros de Custo integrados ao ERP. IAtualizarUsuarioUseCase/ICriarUsuarioUseCase
        // (acima) dependem de ICentroCustoVinculoValidator para a resolução da dívida O1.6-L2 — registrado
        // aqui (e não apenas em AddInfrastructure) para que os dois pontos de composição do host que
        // registram os casos de uso de Usuário (Program.cs e testes de host mínimo) resolvam a árvore de
        // dependências completa.
        services.AddScoped<IFilialErpReader, SomaFilialReader>();
        services.AddScoped<ICentroCustoErpReader, SomaCentroCustoReader>();
        services.AddScoped<IFilialMetadadoRepository, FilialMetadadoRepository>();
        services.AddScoped<ICentroCustoMetadadoRepository, CentroCustoMetadadoRepository>();
        services.AddScoped<IListarFiliaisUseCase, ListarFiliaisUseCase>();
        services.AddScoped<IAtualizarMetadadoFilialUseCase, AtualizarMetadadoFilialUseCase>();
        services.AddScoped<IListarCentrosCustoUseCase, ListarCentrosCustoUseCase>();
        services.AddScoped<IAtualizarMetadadoCentroCustoUseCase, AtualizarMetadadoCentroCustoUseCase>();
        services.AddScoped<ICentroCustoVinculoValidator, CentroCustoVinculoValidator>();

        // O1.8 — Unidades de Alocação (Persistência Real). Sem vínculo com Centro de Custo nesta sprint
        // (escopo da O1.9) e sem integração ERP (ADR-0020, item 4).
        services.AddScoped<IUnidadeAlocacaoRepository, UnidadeAlocacaoRepository>();
        services.AddScoped<IListarUnidadesAlocacaoUseCase, ListarUnidadesAlocacaoUseCase>();
        services.AddScoped<IObterUnidadeAlocacaoUseCase, ObterUnidadeAlocacaoUseCase>();
        services.AddScoped<ICriarUnidadeAlocacaoUseCase, CriarUnidadeAlocacaoUseCase>();
        services.AddScoped<IAtualizarUnidadeAlocacaoUseCase, AtualizarUnidadeAlocacaoUseCase>();
        services.AddScoped<IAlterarStatusUnidadeAlocacaoUseCase, AlterarStatusUnidadeAlocacaoUseCase>();

        // O1.9 — Centro de Custo × Unidade de Alocação (N:N). ICentroCustoMetadadoRepository/
        // ICentroCustoErpReader/IUnidadeAlocacaoRepository já registrados acima — reaproveitados aqui.
        services.AddScoped<ICentroCustoUnidadeAlocacaoRepository, CentroCustoUnidadeAlocacaoRepository>();
        services.AddScoped<IListarVinculosUnidadeAlocacaoUseCase, ListarVinculosUnidadeAlocacaoUseCase>();
        services.AddScoped<ISubstituirVinculosUnidadeAlocacaoUseCase, SubstituirVinculosUnidadeAlocacaoUseCase>();

        // B3 — Bloco 1: Conta Contábil (cadastro de apoio importado do Linx, Discovery homologado).
        services.AddScoped<IContaContabilErpReader, SomaContaContabilReader>();
        services.AddScoped<IContaContabilMetadadoRepository, ContaContabilMetadadoRepository>();
        services.AddScoped<IListarContasContabeisUseCase, ListarContasContabeisUseCase>();
        services.AddScoped<IAtualizarMetadadoContaContabilUseCase, AtualizarMetadadoContaContabilUseCase>();

        // B3 — Bloco 2: Unidade de Medida (cadastro de apoio importado do Linx, Discovery homologado).
        services.AddScoped<IUnidadeMedidaErpReader, SomaUnidadeMedidaReader>();
        services.AddScoped<IUnidadeMedidaMetadadoRepository, UnidadeMedidaMetadadoRepository>();
        services.AddScoped<IListarUnidadesMedidaUseCase, ListarUnidadesMedidaUseCase>();
        services.AddScoped<IAtualizarMetadadoUnidadeMedidaUseCase, AtualizarMetadadoUnidadeMedidaUseCase>();

        // B3 — Bloco 3: Item Fiscal (cadastro local, Discovery homologado). Sem integração Linx nesta
        // etapa — depende apenas dos casos de uso de leitura dos Blocos 1/2 (já registrados acima) para
        // validar Conta Contábil/Unidade de Medida.
        services.AddScoped<IItemFiscalRepository, ItemFiscalRepository>();
        services.AddScoped<IListarItensFiscaisUseCase, ListarItensFiscaisUseCase>();
        services.AddScoped<IObterItemFiscalUseCase, ObterItemFiscalUseCase>();
        services.AddScoped<ICriarItemFiscalUseCase, CriarItemFiscalUseCase>();
        services.AddScoped<IAtualizarItemFiscalUseCase, AtualizarItemFiscalUseCase>();
        services.AddScoped<IAlterarStatusItemFiscalUseCase, AlterarStatusItemFiscalUseCase>();

        // B3 — Bloco 4: Referências de Item Fiscal por Fornecedor (cadastro local, Discovery homologado).
        // Apenas o repositório (sem dependência de Suppliers) vive aqui; os casos de uso — que dependem do
        // cadastro de Fornecedores já existente (IObterFornecedorUseCase) — são registrados em
        // ServiceCollectionExtensions.AddInfrastructure, chamado sempre junto de AddIdentityAuthCore em
        // Program.cs (nunca isoladamente aqui: um teste de composição mínima do host — fail-closed do
        // Bootstrap — só chama AddIdentityAuthCore, e quebraria na validação de DI se a dependência cruzada
        // estivesse aqui).
        services.AddScoped<IItemFiscalReferenciaFornecedorRepository, ItemFiscalReferenciaFornecedorRepository>();

        // O1.11 — Seleção/Cadastro de Unidades de Negócio e Configuração Técnica por Unidade de Negócio.
        // IUnidadeNegocioRepository já é registrado por AddBootstrapCore — reaproveitado aqui.
        // DEB-16 (Gate Final pós-O1.14) — dois protetores distintos, cada um com seu próprio propósito de
        // DataProtection (ver DataProtectionSegredoProtector): nunca o mesmo ISegredoProtector genérico
        // compartilhado entre os domínios de IdentityProvider e ConfiguracaoErp.
        services.AddScoped<IIdentityProviderSegredoProtector, IdentityProviderSegredoProtector>();
        services.AddScoped<IConfiguracaoErpSegredoProtector, ConfiguracaoErpSegredoProtector>();

        services.AddScoped<IListarMinhasUnidadesNegocioUseCase, ListarMinhasUnidadesNegocioUseCase>();
        services.AddScoped<IListarUnidadesNegocioUseCase, ListarUnidadesNegocioUseCase>();
        services.AddScoped<ICriarUnidadeNegocioUseCase, CriarUnidadeNegocioUseCase>();
        services.AddScoped<IRenomearUnidadeNegocioUseCase, RenomearUnidadeNegocioUseCase>();
        services.AddScoped<IAlterarStatusUnidadeNegocioUseCase, AlterarStatusUnidadeNegocioUseCase>();

        services.AddScoped<IIdentityProviderRepository, IdentityProviderRepository>();
        services.AddScoped<IListarIdentityProvidersUseCase, ListarIdentityProvidersUseCase>();
        services.AddScoped<ICriarIdentityProviderUseCase, CriarIdentityProviderUseCase>();
        services.AddScoped<IAtualizarIdentityProviderUseCase, AtualizarIdentityProviderUseCase>();
        services.AddScoped<IAlterarStatusIdentityProviderUseCase, AlterarStatusIdentityProviderUseCase>();

        services.AddScoped<IConfiguracaoErpRepository, ConfiguracaoErpRepository>();
        services.AddScoped<IObterConfiguracaoErpUseCase, ObterConfiguracaoErpUseCase>();
        services.AddScoped<ISalvarConfiguracaoErpUseCase, SalvarConfiguracaoErpUseCase>();
        services.AddScoped<IAlterarStatusConfiguracaoErpUseCase, AlterarStatusConfiguracaoErpUseCase>();

        services.AddScoped<IConfiguracaoNotificacaoRepository, ConfiguracaoNotificacaoRepository>();
        services.AddScoped<IObterConfiguracaoNotificacaoUseCase, ObterConfiguracaoNotificacaoUseCase>();
        services.AddScoped<ISalvarConfiguracaoNotificacaoUseCase, SalvarConfiguracaoNotificacaoUseCase>();

        services.AddScoped<IParametroRepository, ParametroRepository>();
        services.AddScoped<IListarParametrosUseCase, ListarParametrosUseCase>();
        services.AddScoped<ICriarParametroUseCase, CriarParametroUseCase>();
        services.AddScoped<IAtualizarParametroUseCase, AtualizarParametroUseCase>();
        services.AddScoped<IExcluirParametroUseCase, ExcluirParametroUseCase>();

        services.AddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
        services.AddScoped<FeatureFlagProjector>();
        services.AddScoped<IListarFeatureFlagsUseCase, ListarFeatureFlagsUseCase>();
        services.AddScoped<ICriarFeatureFlagUseCase, CriarFeatureFlagUseCase>();
        services.AddScoped<IAlterarStatusFeatureFlagUnidadeUseCase, AlterarStatusFeatureFlagUnidadeUseCase>();

        // O1.12 — Fundação de Administração (Workflow, Alçadas, Controle Orçamentário). Apenas CRUD
        // administrativo por Unidade de Negócio; nenhum motor operacional é registrado aqui.
        services.AddScoped<IRegraWorkflowRepository, RegraWorkflowRepository>();
        services.AddScoped<IListarRegrasWorkflowUseCase, ListarRegrasWorkflowUseCase>();
        services.AddScoped<ICriarRegraWorkflowUseCase, CriarRegraWorkflowUseCase>();
        services.AddScoped<IAtualizarRegraWorkflowUseCase, AtualizarRegraWorkflowUseCase>();
        services.AddScoped<IAlterarStatusRegraWorkflowUseCase, AlterarStatusRegraWorkflowUseCase>();

        services.AddScoped<IAlcadaAprovacaoRepository, AlcadaAprovacaoRepository>();
        services.AddScoped<IListarAlcadasAprovacaoUseCase, ListarAlcadasAprovacaoUseCase>();
        services.AddScoped<ICriarAlcadaAprovacaoUseCase, CriarAlcadaAprovacaoUseCase>();
        services.AddScoped<IAtualizarAlcadaAprovacaoUseCase, AtualizarAlcadaAprovacaoUseCase>();
        services.AddScoped<IAlterarStatusAlcadaAprovacaoUseCase, AlterarStatusAlcadaAprovacaoUseCase>();

        services.AddScoped<IRegraOrcamentariaRepository, RegraOrcamentariaRepository>();
        services.AddScoped<IListarRegrasOrcamentariasUseCase, ListarRegrasOrcamentariasUseCase>();
        services.AddScoped<ICriarRegraOrcamentariaUseCase, CriarRegraOrcamentariaUseCase>();
        services.AddScoped<IAtualizarRegraOrcamentariaUseCase, AtualizarRegraOrcamentariaUseCase>();
        services.AddScoped<IAlterarStatusRegraOrcamentariaUseCase, AlterarStatusRegraOrcamentariaUseCase>();
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

        // Gate Final da Onda 1 (entregável #9) — catálogo inicial de Perfis de negócio, compartilhado
        // pelo Bootstrap e pela criação administrativa de novas Unidades de Negócio.
        services.AddScoped<CatalogoInicialPerfisDeNegocioUseCase>();

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
