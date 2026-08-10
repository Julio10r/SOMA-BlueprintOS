using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

/// <summary>Leitura pública mínima do estado do Bootstrap (security-design-auth-o1.4.md §20.14; Work Order
/// O1.4.3, seção 12). Linha ausente é tratada como fail-closed (indisponível) em runtime, nunca como
/// "disponível por omissão" — a seed migration é a única responsável por criar a linha inicial.</summary>
public sealed class ConsultarBootstrapEstadoUseCase(
    IBootstrapEstadoRepository estados,
    ILogger<ConsultarBootstrapEstadoUseCase> logger) : IConsultarBootstrapEstadoUseCase
{
    public async Task<ConsultarBootstrapEstadoResultado> ExecuteAsync(CancellationToken ct)
    {
        var estado = await estados.ObterAsync(ct);
        if (estado is null)
        {
            logger.LogError(
                "BootstrapEstado ausente — falha operacional (seed não aplicada). Tratado como " +
                "indisponível (fail-closed), nunca como disponível por omissão em runtime.");
            return new ConsultarBootstrapEstadoResultado(Disponivel: false);
        }

        logger.LogInformation("Bootstrap consultado (resultado={Resultado}).", estado.Concluido ? "concluido" : "disponivel");
        return new ConsultarBootstrapEstadoResultado(Disponivel: !estado.Concluido);
    }
}
