using System.Collections.Concurrent;

namespace BlueprintOS.Api.Identity;

/// <summary>Mecanismo de diagnóstico exclusivo de Development para recuperar o OTP em testes locais/E2E,
/// isolado do fluxo normal de autenticação (security-design-auth-o1.4.md, §17.5). Nunca persiste em
/// disco/log/banco — apenas memória de processo, com leitura de uso único e expiração curta. Este tipo
/// só é registrado no container de DI quando <c>IHostEnvironment.IsDevelopment()</c> é verdadeiro
/// (ver <c>AddIdentity</c>) — em qualquer outro ambiente a classe nem existe no processo em execução.</summary>
public sealed class DevelopmentOtpInspectionStore
{
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, (string Codigo, DateTimeOffset ExpiraEm)> _entries = new();
    private readonly TimeProvider _clock;

    public DevelopmentOtpInspectionStore(TimeProvider clock) => _clock = clock;

    public void Store(string email, string codigo) =>
        _entries[Normalize(email)] = (codigo, _clock.GetUtcNow().Add(EntryLifetime));

    /// <summary>Leitura de uso único: remove a entrada ao ser consultada, mesmo em caso de sucesso.</summary>
    public bool TryTakeOnce(string email, out string codigo)
    {
        codigo = string.Empty;
        if (!_entries.TryRemove(Normalize(email), out var entry)) return false;
        if (entry.ExpiraEm < _clock.GetUtcNow()) return false;

        codigo = entry.Codigo;
        return true;
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
