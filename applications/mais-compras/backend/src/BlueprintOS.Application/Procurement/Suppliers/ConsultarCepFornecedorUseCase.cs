using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers;

/// <summary>Gate de homologação de Fornecedores (2026-09-01), item 6: consulta de CEP passa pelo
/// backend (nunca chamada externa direta do frontend), espelhando a arquitetura já existente para
/// consulta de CNPJ (<see cref="ConsultarCnpjFornecedorUseCase"/>). O Linx usa ViaCEP para este
/// mesmo propósito (achado 2, docs/audits/Discovery-Fornecedor-Tela-001016G1.md) — provider
/// distinto do BrasilAPI usado para CNPJ.</summary>
public sealed class ConsultarCepFornecedorUseCase(ICepConsultaProvider provider) : IConsultarCepFornecedorUseCase
{
    public async Task<ConsultaCepResultado> ExecuteAsync(ConsultarCepFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await provider.ConsultarAsync(dto.Cep, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ConsultaCepResultado.CriarFalha(dto.Cep, provider.FonteConsulta, DateTimeOffset.UtcNow, TipoErroConsultaCep.ErroInterno);
        }
    }
}
