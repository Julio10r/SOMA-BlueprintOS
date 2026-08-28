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
        string? snapshotBrutoSanitizado = null;
        var snapshotDescartadoPorTamanho = false;
        try
        {
            // Providers que também sabem produzir um snapshot bruto sanitizado (B2.7/ADR-0023)
            // implementam ICnpjConsultaProviderComSnapshot; o contrato canônico (ConsultaCnpjResultado)
            // nunca depende dessa capacidade opcional — um Provider futuro sem snapshot continua
            // funcionando apenas com ICnpjConsultaProvider, sem qualquer alteração de domínio.
            if (provider is ICnpjConsultaProviderComSnapshot providerComSnapshot)
            {
                var resposta = await providerComSnapshot.ConsultarComSnapshotAsync(dto.Cnpj_Cpf, cancellationToken);
                resultado = resposta.Resultado;
                snapshotBrutoSanitizado = resposta.SnapshotBrutoSanitizado;
                snapshotDescartadoPorTamanho = resposta.SnapshotDescartadoPorTamanho;
            }
            else
            {
                resultado = await provider.ConsultarAsync(dto.Cnpj_Cpf, cancellationToken);
            }
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
        await RegistrarHistoricoAsync(dto, correlationId, resultado, snapshotBrutoSanitizado, snapshotDescartadoPorTamanho, cancellationToken);
        return resultado;
    }

    private async Task RegistrarHistoricoAsync(ConsultarCnpjFornecedorDto dto, string correlationId,
        ConsultaCnpjResultado resultado, string? snapshotBrutoSanitizado, bool snapshotDescartadoPorTamanho,
        CancellationToken cancellationToken)
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
                    resultado.SituacaoCadastral?.ToString() ?? "N/A", resultado.MensagemErro, correlationId, dto.BusinessUnit, dto.ErpSistema,
                    MapearTipoErro(resultado.TipoErro), snapshotBrutoSanitizado, snapshotDescartadoPorTamanho),
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

    /// <summary>Mapeia 1:1, por nome, o enum de erro do Application (<see cref="TipoErroConsultaCnpj"/>)
    /// para o espelho do Domain (<see cref="TipoErroConsultaCnpjHistorico"/>) — o Domain não referencia
    /// o Application layer, então não pode compartilhar o mesmo tipo de enum.</summary>
    private static TipoErroConsultaCnpjHistorico? MapearTipoErro(TipoErroConsultaCnpj? tipoErro) =>
        tipoErro is null ? null : Enum.Parse<TipoErroConsultaCnpjHistorico>(tipoErro.ToString()!);
}
