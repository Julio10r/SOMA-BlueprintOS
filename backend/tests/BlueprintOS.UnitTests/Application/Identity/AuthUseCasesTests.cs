using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Application.Identity;

public sealed class AuthUseCasesTests
{
    [Fact]
    public async Task SolicitarOtp_Should_Return_Same_Response_For_Nonexistent_And_Existing_User()
    {
        var usuarios = new FakeUsuarioRepository();
        var usuario = usuarios.AddAtivo("ana@somagrupo.com.br");
        var codigos = new FakeCodigoRepository();
        var sender = new FakeOtpEmailSender();
        var useCase = CreateSolicitarOtpUseCase(usuarios, codigos, sender);

        var resultadoExistente = await useCase.ExecuteAsync("ana@somagrupo.com.br", CancellationToken.None);
        var resultadoInexistente = await useCase.ExecuteAsync("fantasma@somagrupo.com.br", CancellationToken.None);

        Assert.Equal(resultadoExistente, resultadoInexistente);
        Assert.Single(sender.EnviosPara(usuario.Email));
    }

    [Fact]
    public async Task SolicitarOtp_Should_Not_Send_Email_For_Inactive_User()
    {
        var usuarios = new FakeUsuarioRepository();
        usuarios.AddInativo("inativo@somagrupo.com.br");
        var sender = new FakeOtpEmailSender();
        var useCase = CreateSolicitarOtpUseCase(usuarios, new FakeCodigoRepository(), sender);

        await useCase.ExecuteAsync("inativo@somagrupo.com.br", CancellationToken.None);

        Assert.Empty(sender.EnviosPara("inativo@somagrupo.com.br"));
    }

    [Fact]
    public async Task SolicitarOtp_Should_Invalidate_Previous_Pending_Code()
    {
        var usuarios = new FakeUsuarioRepository();
        var usuario = usuarios.AddAtivo("ana@somagrupo.com.br");
        var codigos = new FakeCodigoRepository();
        var useCase = CreateSolicitarOtpUseCase(usuarios, codigos, new FakeOtpEmailSender());

        await useCase.ExecuteAsync(usuario.Email, CancellationToken.None);
        var primeiro = await codigos.ObterPendentePorUsuarioAsync(usuario.Id, CancellationToken.None);
        await useCase.ExecuteAsync(usuario.Email, CancellationToken.None);
        var segundo = await codigos.ObterPendentePorUsuarioAsync(usuario.Id, CancellationToken.None);

        Assert.NotEqual(primeiro!.Id, segundo!.Id);
        Assert.Equal(StatusCodigoVerificacaoOtp.Expirado, codigos.All.Single(c => c.Id == primeiro.Id).Status);
    }

    [Fact]
    public async Task ValidarOtp_Should_Create_Session_On_Correct_Code()
    {
        var (usuarios, codigos, sessoes, sender, usuario) = await ArrangeWithPendingCode();

        var validar = new ValidarOtpUseCase(usuarios, codigos, sessoes, TimeProvider.System,
            Options.Create(new AuthSessionOptions()), NullLogger<ValidarOtpUseCase>.Instance);
        var resultado = await validar.ExecuteAsync(usuario.Email, sender.LastCodeSent!, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.NotNull(resultado.SessionRawToken);
        Assert.Single(sessoes.All);
    }

    [Fact]
    public async Task ValidarOtp_Should_Reject_Reuse_Of_Consumed_Code()
    {
        var (usuarios, codigos, sessoes, sender, usuario) = await ArrangeWithPendingCode();
        var codigoTexto = sender.LastCodeSent!;
        var validar = new ValidarOtpUseCase(usuarios, codigos, sessoes, TimeProvider.System,
            Options.Create(new AuthSessionOptions()), NullLogger<ValidarOtpUseCase>.Instance);

        var primeira = await validar.ExecuteAsync(usuario.Email, codigoTexto, CancellationToken.None);
        var segunda = await validar.ExecuteAsync(usuario.Email, codigoTexto, CancellationToken.None);

        Assert.True(primeira.Sucesso);
        Assert.False(segunda.Sucesso);
    }

    [Fact]
    public async Task ValidarOtp_Should_Reject_And_Count_Attempts_On_Wrong_Code()
    {
        var (usuarios, codigos, sessoes, _, usuario) = await ArrangeWithPendingCode();
        var validar = new ValidarOtpUseCase(usuarios, codigos, sessoes, TimeProvider.System,
            Options.Create(new AuthSessionOptions()), NullLogger<ValidarOtpUseCase>.Instance);

        var resultado = await validar.ExecuteAsync(usuario.Email, "000000", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        var pendente = await codigos.ObterPendentePorUsuarioAsync(usuario.Id, CancellationToken.None);
        Assert.Equal(1, pendente!.Tentativas);
    }

    [Fact]
    public async Task ValidarOtp_Should_Generate_New_Session_Token_Each_Login_Never_Reusing_Identifier()
    {
        var usuarios = new FakeUsuarioRepository();
        var usuario = usuarios.AddAtivo("ana@somagrupo.com.br");
        var codigos = new FakeCodigoRepository();
        var sessoes = new FakeSessaoRepository();
        var sender = new FakeOtpEmailSender();
        var solicitar = CreateSolicitarOtpUseCase(usuarios, codigos, sender);
        var validar = new ValidarOtpUseCase(usuarios, codigos, sessoes, TimeProvider.System,
            Options.Create(new AuthSessionOptions()), NullLogger<ValidarOtpUseCase>.Instance);

        await solicitar.ExecuteAsync(usuario.Email, CancellationToken.None);
        var primeiroLogin = await validar.ExecuteAsync(usuario.Email, sender.LastCodeSent!, CancellationToken.None);

        await solicitar.ExecuteAsync(usuario.Email, CancellationToken.None);
        var segundoLogin = await validar.ExecuteAsync(usuario.Email, sender.LastCodeSent!, CancellationToken.None);

        Assert.NotEqual(primeiroLogin.SessionRawToken, segundoLogin.SessionRawToken);
    }

    [Fact]
    public async Task ObterIdentidadeAtual_Should_Return_Null_For_Revoked_Session()
    {
        var (usuarios, codigos, sessoes, sender, usuario) = await ArrangeWithPendingCode();
        var validar = new ValidarOtpUseCase(usuarios, codigos, sessoes, TimeProvider.System,
            Options.Create(new AuthSessionOptions()), NullLogger<ValidarOtpUseCase>.Instance);
        var login = await validar.ExecuteAsync(usuario.Email, sender.LastCodeSent!, CancellationToken.None);

        var logout = new LogoutUseCase(sessoes, TimeProvider.System, NullLogger<LogoutUseCase>.Instance);
        await logout.ExecuteAsync(login.SessionRawToken!, CancellationToken.None);

        var obter = new ObterIdentidadeAtualUseCase(sessoes, usuarios, TimeProvider.System, Options.Create(new AuthSessionOptions()));
        var identidade = await obter.ExecuteAsync(login.SessionRawToken!, CancellationToken.None);

        Assert.Null(identidade);
    }

    [Fact]
    public async Task ObterIdentidadeAtual_Should_Return_Null_When_User_Becomes_Inactive()
    {
        var (usuarios, codigos, sessoes, sender, usuario) = await ArrangeWithPendingCode();
        var validar = new ValidarOtpUseCase(usuarios, codigos, sessoes, TimeProvider.System,
            Options.Create(new AuthSessionOptions()), NullLogger<ValidarOtpUseCase>.Instance);
        var login = await validar.ExecuteAsync(usuario.Email, sender.LastCodeSent!, CancellationToken.None);

        usuarios.Inativar(usuario.Id);

        var obter = new ObterIdentidadeAtualUseCase(sessoes, usuarios, TimeProvider.System, Options.Create(new AuthSessionOptions()));
        var identidade = await obter.ExecuteAsync(login.SessionRawToken!, CancellationToken.None);

        Assert.Null(identidade);
    }

    private static async Task<(FakeUsuarioRepository Usuarios, FakeCodigoRepository Codigos, FakeSessaoRepository Sessoes, FakeOtpEmailSender Sender, Usuario Usuario)> ArrangeWithPendingCode()
    {
        var usuarios = new FakeUsuarioRepository();
        var usuario = usuarios.AddAtivo("ana@somagrupo.com.br");
        var codigos = new FakeCodigoRepository();
        var sessoes = new FakeSessaoRepository();
        var sender = new FakeOtpEmailSender();
        var solicitar = CreateSolicitarOtpUseCase(usuarios, codigos, sender);
        await solicitar.ExecuteAsync(usuario.Email, CancellationToken.None);
        return (usuarios, codigos, sessoes, sender, usuario);
    }

    /// <summary>Testes que não focam em throttle usam um throttle sempre-permissivo, para que o
    /// mecanismo de rate limiting por e-mail (Achado A) não interfira com asserções sobre outro
    /// comportamento — o throttle real é exercitado nos testes dedicados em <c>AuthThrottleTests</c>.</summary>
    private static SolicitarOtpUseCase CreateSolicitarOtpUseCase(
        FakeUsuarioRepository usuarios, FakeCodigoRepository codigos, FakeOtpEmailSender sender) =>
        new(usuarios, codigos, new AlwaysAllowOtpRequestThrottleRepository(), sender, TimeProvider.System,
            Options.Create(new OtpRequestThrottleOptions()), NullLogger<SolicitarOtpUseCase>.Instance);
}

internal sealed class AlwaysAllowOtpRequestThrottleRepository : IOtpRequestThrottleRepository
{
    public Task<OtpRequestThrottle?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct) =>
        Task.FromResult<OtpRequestThrottle?>(null);

    public Task AdicionarAsync(OtpRequestThrottle throttle, CancellationToken ct) => Task.CompletedTask;

    public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FakeUsuarioRepository : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = [];

    public Usuario AddAtivo(string email)
    {
        var usuario = new Usuario(email, "Usuária de Teste", Guid.NewGuid());
        _usuarios.Add(usuario);
        return usuario;
    }

    public Task AdicionarAsync(Usuario usuario, CancellationToken ct)
    {
        _usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public Usuario AddInativo(string email)
    {
        var usuario = AddAtivo(email);
        Inativar(usuario.Id);
        return usuario;
    }

    public void Inativar(Guid id)
    {
        var usuario = _usuarios.Single(x => x.Id == id);
        typeof(Usuario).GetProperty(nameof(Usuario.Status))!.SetValue(usuario, StatusUsuario.Inativo);
    }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) =>
        Task.FromResult(_usuarios.SingleOrDefault(x => x.Email == email));

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_usuarios.SingleOrDefault(x => x.Id == id));
}

internal sealed class FakeCodigoRepository : ICodigoVerificacaoOtpRepository
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

internal sealed class FakeSessaoRepository : ISessaoAutenticacaoRepository
{
    public List<SessaoAutenticacao> All { get; } = [];

    public Task<SessaoAutenticacao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct) =>
        Task.FromResult(All.SingleOrDefault(x => x.IdentificadorHash == identificadorHash));

    public Task AdicionarAsync(SessaoAutenticacao sessao, CancellationToken ct)
    {
        All.Add(sessao);
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(SessaoAutenticacao sessao, CancellationToken ct) => Task.CompletedTask;

    public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
}

/// <summary>Fake de teste: captura o código apenas em memória de teste — equivalente conceitual ao
/// mecanismo de diagnóstico de Development, mas isolado do código de produção.</summary>
internal sealed class FakeOtpEmailSender : IOtpEmailSender
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
