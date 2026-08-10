using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>Testes de concorrência REAL (O1.4.2.1, Etapa 2) — usam o provider InMemory do EF Core com
/// múltiplas instâncias de <see cref="BlueprintOSDbContext"/> compartilhando o mesmo banco nomeado, para
/// simular duas requisições HTTP concorrentes (cada uma com seu próprio DbContext por escopo, como em
/// produção). Não usam fakes sequenciais — o objetivo é exercitar o RowVersion/índice único reais.
///
/// LIMITAÇÃO DOCUMENTADA: o provider InMemory não avalia a cláusula de filtro de índices únicos
/// (<c>HasFilter</c> é uma API relacional). O índice único configurado em
/// <c>CodigoVerificacaoOtpConfiguration</c> é filtrado por <c>Status = 0</c> apenas em SQL Server real;
/// sob InMemory ele se comporta como um índice único incondicional sobre <c>UsuarioId</c>. Isso não
/// invalida os testes abaixo (eles ainda provam "no máximo um código pendente sobrevive à corrida"),
/// mas a garantia de que apenas o filtro por Status é respeitado em produção não foi comprovada
/// empiricamente aqui — apenas por leitura de código/configuração. Validar contra SQL Server real
/// quando o ambiente compartilhado estiver disponível (ver limitação de banco registrada na Security
/// Validation).</summary>
public sealed class AuthConcurrencyTests
{
    [Fact]
    public async Task ValidarOtp_Concurrent_Same_Code_Should_Yield_Exactly_One_Success_And_One_Session()
    {
        var dbName = Guid.NewGuid().ToString();
        var usuarioId = Guid.NewGuid();
        var unidadeNegocioId = Guid.NewGuid();
        const string email = "concorrencia@somagrupo.com.br";
        const string codigoTexto = "482913";

        await using (var setupDb = CreateContext(dbName))
        {
            var usuario = new Usuario(email, "Usuária Concorrência", unidadeNegocioId);
            typeof(Usuario).GetProperty(nameof(Usuario.Id))!.SetValue(usuario, usuarioId);
            setupDb.Usuarios.Add(usuario);

            var (hash, salt) = BlueprintOS.Application.Identity.Security.OtpHasher.Hash(codigoTexto);
            setupDb.CodigosVerificacaoOtp.Add(new CodigoVerificacaoOtp(usuarioId, hash, salt, DateTimeOffset.UtcNow));
            await setupDb.SaveChangesAsync();
        }

        Task<ValidarOtpResultado> ExecutarValidacaoAsync()
        {
            var db = CreateContext(dbName);
            var useCase = new ValidarOtpUseCase(
                new UsuarioRepository(db),
                new CodigoVerificacaoOtpRepository(db),
                new SessaoAutenticacaoRepository(db),
                TimeProvider.System,
                Options.Create(new AuthSessionOptions()),
                NullLogger<ValidarOtpUseCase>.Instance);
            return useCase.ExecuteAsync(email, codigoTexto, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(ExecutarValidacaoAsync(), ExecutarValidacaoAsync());

        Assert.Single(resultados, r => r.Sucesso);
        Assert.Single(resultados, r => !r.Sucesso);

        await using var assertDb = CreateContext(dbName);
        Assert.Equal(1, await assertDb.SessoesAutenticacao.CountAsync());
    }

    [Fact]
    public async Task SolicitarOtp_Concurrent_Resend_Should_Result_In_At_Most_One_Pending_Code()
    {
        var dbName = Guid.NewGuid().ToString();
        var usuarioId = Guid.NewGuid();
        const string email = "reenvio-concorrente@somagrupo.com.br";

        await using (var setupDb = CreateContext(dbName))
        {
            var usuario = new Usuario(email, "Usuária Reenvio", Guid.NewGuid());
            typeof(Usuario).GetProperty(nameof(Usuario.Id))!.SetValue(usuario, usuarioId);
            setupDb.Usuarios.Add(usuario);
            await setupDb.SaveChangesAsync();
        }

        // Throttle deliberadamente permissivo aqui — o objetivo deste teste é isolar a corrida ao
        // nível do código OTP (índice único), não a corrida de throttle (coberta pelo teste seguinte).
        var throttlePermissivo = Options.Create(new OtpRequestThrottleOptions { CooldownSegundos = 0, MaxSolicitacoesPorJanela = 1000 });

        Task ExecutarSolicitacaoAsync()
        {
            var db = CreateContext(dbName);
            var useCase = new SolicitarOtpUseCase(
                new UsuarioRepository(db),
                new CodigoVerificacaoOtpRepository(db),
                new OtpRequestThrottleRepository(db),
                new NoOpOtpEmailSender(),
                TimeProvider.System,
                throttlePermissivo,
                NullLogger<SolicitarOtpUseCase>.Instance);
            return useCase.ExecuteAsync(email, CancellationToken.None);
        }

        await Task.WhenAll(ExecutarSolicitacaoAsync(), ExecutarSolicitacaoAsync());

        await using var assertDb = CreateContext(dbName);
        var pendentes = await assertDb.CodigosVerificacaoOtp
            .Where(x => x.UsuarioId == usuarioId && x.Status == StatusCodigoVerificacaoOtp.Pendente)
            .CountAsync();

        Assert.True(pendentes <= 1, $"Esperado no máximo 1 código pendente após corrida; encontrados {pendentes}.");
    }

    [Fact]
    public async Task SolicitarOtp_Concurrent_Requests_Same_Email_Should_Create_At_Most_One_Code_Under_Real_Throttle()
    {
        var dbName = Guid.NewGuid().ToString();
        var usuarioId = Guid.NewGuid();
        const string email = "throttle-concorrente@somagrupo.com.br";

        await using (var setupDb = CreateContext(dbName))
        {
            var usuario = new Usuario(email, "Usuária Throttle", Guid.NewGuid());
            typeof(Usuario).GetProperty(nameof(Usuario.Id))!.SetValue(usuario, usuarioId);
            setupDb.Usuarios.Add(usuario);
            await setupDb.SaveChangesAsync();
        }

        var throttleReal = Options.Create(new OtpRequestThrottleOptions());

        Task ExecutarSolicitacaoAsync()
        {
            var db = CreateContext(dbName);
            var useCase = new SolicitarOtpUseCase(
                new UsuarioRepository(db),
                new CodigoVerificacaoOtpRepository(db),
                new OtpRequestThrottleRepository(db),
                new NoOpOtpEmailSender(),
                TimeProvider.System,
                throttleReal,
                NullLogger<SolicitarOtpUseCase>.Instance);
            return useCase.ExecuteAsync(email, CancellationToken.None);
        }

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => ExecutarSolicitacaoAsync()));

        await using var assertDb = CreateContext(dbName);
        var codigosCriados = await assertDb.CodigosVerificacaoOtp.CountAsync(x => x.UsuarioId == usuarioId);

        // Sob corrida (todas as 5 requisições essencialmente simultâneas, dentro do cooldown de 60s uma
        // da outra), no máximo 1 código deveria ter sido efetivamente criado — o throttle nega as
        // demais antes de qualquer tentativa de criação de código.
        Assert.True(codigosCriados <= 1, $"Esperado no máximo 1 código criado sob corrida de throttle; encontrados {codigosCriados}.");
    }

    private static BlueprintOSDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new BlueprintOSDbContext(options);
    }

    private sealed class NoOpOtpEmailSender : BlueprintOS.Application.Identity.Contracts.IOtpEmailSender
    {
        public Task<BlueprintOS.Application.Identity.Contracts.OtpEmailSendResult> SendAsync(string email, string codigo, CancellationToken ct) =>
            Task.FromResult(new BlueprintOS.Application.Identity.Contracts.OtpEmailSendResult(true, null));
    }
}
