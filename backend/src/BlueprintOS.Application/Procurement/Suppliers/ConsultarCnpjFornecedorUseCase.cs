using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class ConsultarCnpjFornecedorUseCase(
    ICnpjConsultaProvider provider,
    IFornecedorCnpjConsultaHistoricoRepository historicoRepository,
    ICurrentIdentity identity) : IConsultarCnpjFornecedorUseCase
{
    public async Task<ConsultaCnpjResultado> ExecuteAsync(ConsultarCnpjFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var correlationId = string.IsNullOrWhiteSpace(dto.CorrelationId) ? Guid.NewGuid().ToString("N") : dto.CorrelationId.Trim();
        try
        {
            var resultado = await provider.ConsultarAsync(dto.Cnpj_Cpf, cancellationToken);
            await RegistrarHistoricoAsync(dto, correlationId, resultado, cancellationToken);
            return resultado;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            var resultado = ConsultaCnpjResultado.CriarFalha(dto.Cnpj_Cpf, provider.FonteConsulta, DateTimeOffset.UtcNow,
                "Falha ao consultar a fonte externa.");
            await RegistrarHistoricoAsync(dto, correlationId, resultado, cancellationToken);
            return resultado;
        }
    }

    private Task RegistrarHistoricoAsync(ConsultarCnpjFornecedorDto dto, string correlationId,
        ConsultaCnpjResultado resultado, CancellationToken cancellationToken) => historicoRepository.AdicionarAsync(
        new FornecedorCnpjConsultaHistorico(Guid.NewGuid(), resultado.Cnpj_Cpf, resultado.FonteConsulta,
            resultado.DataConsulta, identity.GetRequired().UserId, resultado.StatusConsulta.ToString(),
            resultado.SituacaoCadastral.ToString(), resultado.MensagemErro, correlationId, dto.BusinessUnit, dto.ErpSistema), cancellationToken);
}
