using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class ConsultarCnpjFornecedorUseCase(
    ICnpjConsultaProvider provider,
    IFornecedorCnpjConsultaHistoricoRepository historicoRepository,
    ICurrentIdentity identity,
    ILogger<ConsultarCnpjFornecedorUseCase> logger) : IConsultarCnpjFornecedorUseCase
{
    public async Task<ConsultaCnpjResultado> ExecuteAsync(ConsultarCnpjFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var correlationId = string.IsNullOrWhiteSpace(dto.CorrelationId) ? Guid.NewGuid().ToString("N") : dto.CorrelationId.Trim();
        ConsultaCnpjResultado resultado;
        try
        {
            resultado = await provider.ConsultarAsync(dto.Cnpj_Cpf, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            resultado = ConsultaCnpjResultado.CriarFalha(dto.Cnpj_Cpf, provider.FonteConsulta, DateTimeOffset.UtcNow,
                TipoErroConsultaCnpj.ErroInterno);
        }

        // O registro de historico e uma acao auxiliar de auditoria: uma falha ao
        // persisti-lo (ex: banco corporativo indisponivel) nao pode derrubar a
        // resposta da consulta em si, que ja foi obtida com sucesso ou tratada acima.
        await RegistrarHistoricoAsync(dto, correlationId, resultado, cancellationToken);
        return resultado;
    }

    private async Task RegistrarHistoricoAsync(ConsultarCnpjFornecedorDto dto, string correlationId,
        ConsultaCnpjResultado resultado, CancellationToken cancellationToken)
    {
        try
        {
            var userId = identity.GetRequired().UserId;
            await historicoRepository.AdicionarAsync(
                new FornecedorCnpjConsultaHistorico(Guid.NewGuid(), resultado.Cnpj_Cpf, resultado.FonteConsulta,
                    resultado.DataConsulta, userId, resultado.StatusConsulta.ToString(),
                    // "Resultado" é um campo de auditoria em texto livre (nunca o contrato canônico exposto
                    // ao frontend); SituacaoCadastral só existe em consultas bem-sucedidas — "N/A" documenta
                    // a ausência sem reintroduzir um valor de enum sobrecarregado para representar falha.
                    resultado.SituacaoCadastral?.ToString() ?? "N/A", resultado.MensagemErro, correlationId, dto.BusinessUnit, dto.ErpSistema),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Falha ao registrar historico de consulta de CNPJ. CorrelationId {CorrelationId}. Cnpj {Cnpj}.",
                correlationId, resultado.Cnpj_Cpf);
        }
    }
}
