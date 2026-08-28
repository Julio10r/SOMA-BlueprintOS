using BlueprintOS.Application.Identity.Contracts;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.Api.Identity;

/// <summary>Estritamente exclusivo de Development (security-design-auth-o1.4.md, §17.3). Nunca envia
/// e-mail real; nunca loga o código; grava apenas no <see cref="DevelopmentOtpInspectionStore"/> em
/// memória para o mecanismo de diagnóstico de testes locais/E2E. A dupla checagem de ambiente aqui
/// (registro condicional no DI + guarda interna) é defesa em profundidade — não substitui a seleção
/// por <see cref="IHostEnvironment"/> feita na composição raiz.</summary>
public sealed class DevelopmentOtpEmailSender(DevelopmentOtpInspectionStore store, IHostEnvironment environment) : IOtpEmailSender
{
    public Task<OtpEmailSendResult> SendAsync(string email, string codigo, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "DevelopmentOtpEmailSender não pode ser utilizado fora de Development.");
        }

        store.Store(email, codigo);
        return Task.FromResult(new OtpEmailSendResult(true, null));
    }
}
