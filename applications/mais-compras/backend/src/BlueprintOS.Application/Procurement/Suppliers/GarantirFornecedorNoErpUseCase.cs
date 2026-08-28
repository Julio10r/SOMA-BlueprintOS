using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed record GarantirFornecedorNoErpDto(string? CorrelationId);

public interface IGarantirFornecedorNoErpUseCase
{
    Task<GarantirFornecedorErpResultado?> ExecuteAsync(Guid fornecedorId, string businessUnit, GarantirFornecedorNoErpDto dto, CancellationToken cancellationToken = default);
}

/// <summary>Ponto de entrada de negócio para "garantir/atualizar fornecedor no ERP" (ADR-0023, B2.9).
/// Nunca chamado a partir de uma consulta de CNPJ (B2.6) — só a partir de uma operação explícita do usuário
/// sobre um Fornecedor já existente no domínio +Compras. Consultar não é persistir; este use case é o único
/// caminho que escreve no ERP.</summary>
public sealed class GarantirFornecedorNoErpUseCase(
    IFornecedorRepository fornecedores,
    IGarantirFornecedorErpAdapterResolver resolver,
    ICurrentIdentity identity) : IGarantirFornecedorNoErpUseCase
{
    public async Task<GarantirFornecedorErpResultado?> ExecuteAsync(Guid fornecedorId, string businessUnit, GarantirFornecedorNoErpDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessUnit)) throw new ErpFornecedorEscritaException(ErpFornecedorErro.Validacao, "A Unidade de Negócio (BU) é obrigatória para integração com o ERP.");

        var requestIdentity = identity.GetRequired();
        var fornecedor = await fornecedores.ObterPorIdAsync(fornecedorId, requestIdentity.UserId, cancellationToken);
        if (fornecedor is null) return null;

        var correlationId = string.IsNullOrWhiteSpace(dto.CorrelationId) ? Guid.NewGuid().ToString("N") : dto.CorrelationId.Trim();
        var adapter = resolver.Resolver(businessUnit);

        var request = new GarantirFornecedorErpRequest(
            BusinessUnit: businessUnit.Trim(),
            DocumentoFiscal: fornecedor.Cnpj_Cpf,
            Nome: fornecedor.RazaoSocial,
            RazaoSocial: fornecedor.RazaoSocial,
            Cidade: fornecedor.Cidade,
            Estado: fornecedor.Estado,
            Pais: fornecedor.Pais,
            Ativo: string.Equals(fornecedor.Status, "Ativo", StringComparison.OrdinalIgnoreCase),
            CorrelationId: correlationId);

        var resultado = await adapter.GarantirAsync(request, cancellationToken);

        fornecedor.RegistrarVinculoErp(resultado.BusinessUnit, resultado.ErpSistema, resultado.IdentificadorExterno);
        fornecedor.RegistrarSincronizacao("Sincronizado", resultado.ProcessadoEm);
        await fornecedores.AtualizarAsync(fornecedor, cancellationToken);

        return resultado;
    }
}
