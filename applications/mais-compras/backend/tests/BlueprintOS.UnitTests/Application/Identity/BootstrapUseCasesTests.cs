using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Identity.Security;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>Cobre o subconjunto do plano de testes da Work Order O1.4.3 (seção 18) atribuído a O1.4.3.1:
/// itens 1 (disponibilidade inicial), 2 (secret inválido), 3 (identidade não autorizada), 4 (OTP inválido),
/// 5 (OTP expirado), 6 (OTP replay), 13 (Concluido=true bloqueia /bootstrap/iniciar), 18/20/21 (fail-closed
/// de Options). Itens 7 (rate limiting completo)/8/9/10/16/19 são cobertos em
/// <c>BootstrapAuthorizationPipelineTests</c> (nível de pipeline HTTP); itens 11/12/14/15/17/22 pertencem a
/// O1.4.3.2 (conclusão transacional) e não são implementados aqui.</summary>
public sealed class BootstrapUseCasesTests
{
    private const string SecretValido = "segredo-bootstrap-de-alta-entropia";
    private const string EmailAutorizado = "admin.inicial@example.invalid";

    [Fact]
    public async Task ConsultarEstado_Should_Return_Disponivel_True_When_Seed_Not_Concluded()
    {
        var estados = new FakeBootstrapEstadoRepository(BootstrapEstado.CriarInicial());
        var useCase = new ConsultarBootstrapEstadoUseCase(estados, NullLogger<ConsultarBootstrapEstadoUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.True(resultado.Disponivel);
    }

    [Fact]
    public async Task ConsultarEstado_Should_Fail_Closed_When_Row_Is_Missing()
    {
        var estados = new FakeBootstrapEstadoRepository(estadoAusente: true);
        var useCase = new ConsultarBootstrapEstadoUseCase(estados, NullLogger<ConsultarBootstrapEstadoUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.False(resultado.Disponivel);
    }

    [Fact]
    public async Task Iniciar_Should_Return_Generic_Success_Response_For_Invalid_Secret()
    {
        var (useCase, codigos, sender) = CreateIniciarUseCase();

        var resultado = await useCase.ExecuteAsync(EmailAutorizado, "secret-errado", CancellationToken.None);

        Assert.True(resultado.BootstrapDisponivel);
        Assert.Empty(codigos.All);
        Assert.Empty(sender.EnviosPara(EmailAutorizado));
    }

    [Fact]
    public async Task Iniciar_Should_Return_Generic_Success_Response_For_Unauthorized_Email_With_Same_Shape_As_Invalid_Secret()
    {
        var (useCase, codigos, _) = CreateIniciarUseCase();

        var comSecretInvalido = await useCase.ExecuteAsync(EmailAutorizado, "secret-errado", CancellationToken.None);
        var comEmailNaoAutorizado = await useCase.ExecuteAsync("estranho@somagrupo.com.br", SecretValido, CancellationToken.None);

        // Mesma "forma" de resposta (mesmo tipo/campo) para os dois casos de rejeição — nunca uma
        // distinção observável entre "secret inválido" e "e-mail não autorizado" (security-design-auth-o1.4.md §20.6).
        Assert.Equal(comSecretInvalido, comEmailNaoAutorizado);
        Assert.Empty(codigos.All);
    }

    [Fact]
    public async Task Iniciar_Should_Issue_Otp_When_Secret_And_Email_Are_Valid()
    {
        var (useCase, codigos, sender) = CreateIniciarUseCase();

        var resultado = await useCase.ExecuteAsync(EmailAutorizado, SecretValido, CancellationToken.None);

        Assert.True(resultado.BootstrapDisponivel);
        Assert.Single(codigos.All);
        Assert.Null(codigos.All[0].UsuarioId);
        Assert.Equal(EmailAutorizado, codigos.All[0].EmailCandidato);
        Assert.Single(sender.EnviosPara(EmailAutorizado));
    }

    [Fact]
    public async Task Iniciar_Should_Return_BootstrapDisponivel_False_When_Already_Concluded()
    {
        var estado = BootstrapEstado.CriarInicial();
        var estados = new FakeBootstrapEstadoRepository(estado);
        MarcarConcluido(estado);
        var (useCase, _, _) = CreateIniciarUseCase(estados);

        var resultado = await useCase.ExecuteAsync(EmailAutorizado, SecretValido, CancellationToken.None);

        // Item 13 do plano de testes — controller traduz este resultado em 404 indistinguível de rota
        // inexistente, nunca avaliando secret/e-mail (security-design-auth-o1.4.md §20.10).
        Assert.False(resultado.BootstrapDisponivel);
    }

    [Fact]
    public async Task Iniciar_Should_Fail_Closed_When_BootstrapEstado_Row_Is_Missing()
    {
        var estados = new FakeBootstrapEstadoRepository(estadoAusente: true);
        var (useCase, _, _) = CreateIniciarUseCase(estados);

        var resultado = await useCase.ExecuteAsync(EmailAutorizado, SecretValido, CancellationToken.None);

        Assert.False(resultado.BootstrapDisponivel);
    }

    [Fact]
    public async Task Iniciar_Should_Never_Treat_Empty_Configured_Secret_As_Always_Valid()
    {
        var (useCase, codigos, _) = CreateIniciarUseCase(secretConfigurado: "");

        var resultado = await useCase.ExecuteAsync(EmailAutorizado, "", CancellationToken.None);

        Assert.True(resultado.BootstrapDisponivel); // resposta genérica, mas...
        Assert.Empty(codigos.All); // ...nenhum OTP foi de fato emitido — secret vazio nunca é "sempre válido".
    }

    [Fact]
    public async Task Iniciar_Should_Fail_Closed_When_Allowlist_Is_Empty()
    {
        var (useCase, codigos, _) = CreateIniciarUseCase(emailsAutorizados: Array.Empty<string>());

        await useCase.ExecuteAsync(EmailAutorizado, SecretValido, CancellationToken.None);

        Assert.Empty(codigos.All);
    }

    [Fact]
    public async Task VerificarOtp_Should_Reject_Invalid_Code()
    {
        var (validar, codigos, sessoes, _) = await ArrangeComOtpEmitidoAsync();

        var resultado = await validar.ExecuteAsync(EmailAutorizado, "000000", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Null(resultado.SessionRawToken);
        Assert.Empty(sessoes.All);
        var pendente = codigos.All.Single();
        Assert.Equal(1, pendente.Tentativas);
    }

    [Fact]
    public async Task VerificarOtp_Should_Reject_Expired_Code()
    {
        var codigos = new FakeCodigoRepositoryBootstrap();
        var estados = new FakeBootstrapEstadoRepository(BootstrapEstado.CriarInicial());
        var sessoes = new FakeBootstrapSessaoRepository();
        var criadoEm = DateTimeOffset.UtcNow - CodigoVerificacaoOtp.Validade - TimeSpan.FromMinutes(1);
        var (hash, salt) = OtpHasher.Hash("123456");
        codigos.All.Add(CodigoVerificacaoOtp.ParaCandidatoBootstrap(EmailAutorizado, hash, salt, criadoEm));
        var validar = new ValidarOtpBootstrapUseCase(estados, codigos, sessoes, TimeProvider.System, NullLogger<ValidarOtpBootstrapUseCase>.Instance);

        var resultado = await validar.ExecuteAsync(EmailAutorizado, "123456", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Empty(sessoes.All);
    }

    [Fact]
    public async Task VerificarOtp_Should_Reject_Replay_Of_Already_Consumed_Code()
    {
        var (validar, _, sessoes, codigoTexto) = await ArrangeComOtpEmitidoAsync();

        var primeira = await validar.ExecuteAsync(EmailAutorizado, codigoTexto, CancellationToken.None);
        var segunda = await validar.ExecuteAsync(EmailAutorizado, codigoTexto, CancellationToken.None);

        Assert.True(primeira.Sucesso);
        Assert.False(segunda.Sucesso);
        Assert.Single(sessoes.All);
    }

    [Fact]
    public async Task VerificarOtp_Should_Create_BootstrapSessao_Without_UsuarioId_On_Success()
    {
        var (validar, _, sessoes, codigoTexto) = await ArrangeComOtpEmitidoAsync();

        var resultado = await validar.ExecuteAsync(EmailAutorizado, codigoTexto, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(resultado.SessionRawToken);
        Assert.Equal(EmailAutorizado, resultado.EmailCandidato);
        Assert.Single(sessoes.All);
        Assert.Equal(EmailAutorizado, sessoes.All[0].EmailCandidato);
    }

    private static void MarcarConcluido(BootstrapEstado estado)
    {
        typeof(BootstrapEstado).GetProperty(nameof(BootstrapEstado.Concluido))!.SetValue(estado, true);
    }

    private static (IniciarBootstrapUseCase UseCase, FakeCodigoRepositoryBootstrap Codigos, FakeOtpEmailSenderBootstrap Sender) CreateIniciarUseCase(
        FakeBootstrapEstadoRepository? estados = null,
        string? secretConfigurado = SecretValido,
        string[]? emailsAutorizados = null)
    {
        estados ??= new FakeBootstrapEstadoRepository(BootstrapEstado.CriarInicial());
        var sessoes = new FakeBootstrapSessaoRepository();
        var codigos = new FakeCodigoRepositoryBootstrap();
        var sender = new FakeOtpEmailSenderBootstrap();
        var useCase = new IniciarBootstrapUseCase(
            estados,
            sessoes,
            codigos,
            new AlwaysAllowOtpRequestThrottleRepository(),
            sender,
            TimeProvider.System,
            Options.Create(new BootstrapSecretOptions { Secret = secretConfigurado }),
            Options.Create(new BootstrapAllowedCandidatesOptions { Emails = emailsAutorizados ?? new[] { EmailAutorizado } }),
            Options.Create(new OtpRequestThrottleOptions()),
            NullLogger<IniciarBootstrapUseCase>.Instance);

        return (useCase, codigos, sender);
    }

    private static async Task<(ValidarOtpBootstrapUseCase Validar, FakeCodigoRepositoryBootstrap Codigos, FakeBootstrapSessaoRepository Sessoes, string CodigoTexto)> ArrangeComOtpEmitidoAsync()
    {
        var (iniciar, codigos, sender) = CreateIniciarUseCase();
        await iniciar.ExecuteAsync(EmailAutorizado, SecretValido, CancellationToken.None);

        var estados = new FakeBootstrapEstadoRepository(BootstrapEstado.CriarInicial());
        var sessoes = new FakeBootstrapSessaoRepository();
        var validar = new ValidarOtpBootstrapUseCase(estados, codigos, sessoes, TimeProvider.System, NullLogger<ValidarOtpBootstrapUseCase>.Instance);

        return (validar, codigos, sessoes, sender.LastCodeSent!);
    }
}

internal sealed class FakeBootstrapEstadoRepository : IBootstrapEstadoRepository
{
    private readonly BootstrapEstado? _estado;

    public FakeBootstrapEstadoRepository(BootstrapEstado? estado = null, bool estadoAusente = false)
    {
        _estado = estadoAusente ? null : estado;
    }

    public Task<BootstrapEstado?> ObterAsync(CancellationToken ct) => Task.FromResult(_estado);
    public Task AtualizarAsync(BootstrapEstado estado, CancellationToken ct) => Task.CompletedTask;
    public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FakeBootstrapSessaoRepository : IBootstrapSessaoRepository
{
    public List<BootstrapSessao> All { get; } = [];

    public Task<BootstrapSessao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct) =>
        Task.FromResult(All.SingleOrDefault(x => x.IdentificadorHash == identificadorHash));

    public Task<BootstrapSessao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(All.SingleOrDefault(x => x.Id == id));

    public Task<BootstrapSessao?> ObterAtivaPorEmailCandidatoAsync(string emailCandidato, CancellationToken ct) =>
        Task.FromResult(All.Where(x => x.EmailCandidato == emailCandidato && x.UsadaEm == null && x.RevokedAt == null)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefault());

    public Task AdicionarAsync(BootstrapSessao sessao, CancellationToken ct)
    {
        All.Add(sessao);
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(BootstrapSessao sessao, CancellationToken ct) => Task.CompletedTask;

    public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FakeCodigoRepositoryBootstrap : ICodigoVerificacaoOtpRepository
{
    public List<CodigoVerificacaoOtp> All { get; } = [];

    public Task<CodigoVerificacaoOtp?> ObterPendentePorUsuarioAsync(Guid usuarioId, CancellationToken ct) =>
        Task.FromResult(All.Where(x => x.UsuarioId == usuarioId && x.Status == StatusCodigoVerificacaoOtp.Pendente)
            .OrderByDescending(x => x.CriadoEm).FirstOrDefault());

    public Task<CodigoVerificacaoOtp?> ObterMaisRecentePorUsuarioAsync(Guid usuarioId, CancellationToken ct) =>
        Task.FromResult(All.Where(x => x.UsuarioId == usuarioId).OrderByDescending(x => x.CriadoEm).FirstOrDefault());

    public Task<CodigoVerificacaoOtp?> ObterPendentePorEmailCandidatoAsync(string emailCandidato, CancellationToken ct) =>
        Task.FromResult(All.Where(x => x.EmailCandidato == emailCandidato && x.Status == StatusCodigoVerificacaoOtp.Pendente)
            .OrderByDescending(x => x.CriadoEm).FirstOrDefault());

    public Task AdicionarAsync(CodigoVerificacaoOtp codigo, CancellationToken ct)
    {
        All.Add(codigo);
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(CodigoVerificacaoOtp codigo, CancellationToken ct) => Task.CompletedTask;

    public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FakeOtpEmailSenderBootstrap : IOtpEmailSender
{
    private readonly List<(string Email, string Codigo)> _envios = [];
    public string? LastCodeSent { get; private set; }

    public Task<OtpEmailSendResult> SendAsync(string email, string codigo, CancellationToken ct)
    {
        _envios.Add((email, codigo));
        LastCodeSent = codigo;
        return Task.FromResult(new OtpEmailSendResult(true, null));
    }

    public IReadOnlyList<(string Email, string Codigo)> EnviosPara(string email) =>
        _envios.Where(x => x.Email == email).ToList();
}
