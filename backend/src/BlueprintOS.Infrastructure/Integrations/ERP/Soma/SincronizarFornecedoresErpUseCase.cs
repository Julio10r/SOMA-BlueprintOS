using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

public sealed class SincronizarFornecedoresErpUseCase(
    IFornecedorErpReader reader,
    IFornecedorRepository repository,
    ICurrentIdentity identity) : ISincronizarFornecedoresErpUseCase
{
    public async Task<SincronizacaoFornecedoresErpResumo> ExecuteAsync(SincronizarFornecedoresErpDto dto, CancellationToken cancellationToken = default)
    {
        var userId = identity.GetRequired().UserId;
        var correlationId = string.IsNullOrWhiteSpace(dto.CorrelationId) ? Guid.NewGuid().ToString("N") : dto.CorrelationId.Trim()[..Math.Min(dto.CorrelationId.Trim().Length, 100)];
        var businessUnit = string.IsNullOrWhiteSpace(dto.BusinessUnit) ? "DEFAULT" : dto.BusinessUnit.Trim();
        var consultados = await reader.BuscarFornecedoresAsync(dto.Limite, cancellationToken);
        var incluidos = 0;
        var atualizados = 0;
        var semAlteracao = 0;

        foreach (var externo in consultados)
        {
            var dados = externo.Dados;
            if (string.IsNullOrWhiteSpace(dados.RazaoSocial) || string.IsNullOrWhiteSpace(dados.DocumentoFiscal)) continue;

            var local = await repository.ObterPorCnpjAsync(DocumentoFiscal.Create(dados.DocumentoFiscal).Value, userId, cancellationToken);
            var alteradoEm = externo.UltimaAlteracaoEm ?? DateTimeOffset.UtcNow;
            if (local is null)
            {
                local = new Fornecedor(Guid.NewGuid(), dados.RazaoSocial, DocumentoFiscal.Create(dados.DocumentoFiscal), dados.TipoPessoa, null,
                    dados.EmailComercial, dados.Telefone, null, dados.Cidade, dados.Uf, dados.Pais, dados.Ativo ? "Ativo" : "Inativo", null,
                    userId, alteradoEm, businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
                local.AplicarContratoCanonico(dados, "ERP", alteradoEm);
                local.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
                local.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
                await repository.AdicionarAsync(local, cancellationToken);
                incluidos++;
                continue;
            }

            local.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            if (EstaSemAlteracao(local, dados))
            {
                local.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
                await repository.AtualizarAsync(local, cancellationToken);
                semAlteracao++;
                continue;
            }

            local.AplicarContratoCanonico(dados, "ERP", alteradoEm);
            local.RegistrarVinculoErp(businessUnit, externo.ErpSistema, externo.ErpFornecedorId);
            local.RegistrarSincronizacao("Sincronizado", DateTimeOffset.UtcNow);
            await repository.AtualizarAsync(local, cancellationToken);
            atualizados++;
        }

        return new(consultados.Count, incluidos, atualizados, semAlteracao, businessUnit, "SOMA_DESENV", correlationId, DateTimeOffset.UtcNow);
    }

    private static bool EstaSemAlteracao(Fornecedor local, FornecedorCanonico dados)
    {
        if (!string.IsNullOrWhiteSpace(local.HashDadosSincronizaveis) && local.HashDadosSincronizaveis == dados.HashDadosSincronizaveis) return true;
        return local.RazaoSocial == dados.RazaoSocial.Trim()
            && local.Cnpj_Cpf == DocumentoFiscal.Create(dados.DocumentoFiscal).Value
            && local.NomeFantasia == dados.NomeFantasia?.Trim()
            && local.TipoPessoa == dados.TipoPessoa?.Trim()
            && local.Cidade == dados.Cidade
            && local.Estado == dados.Uf
            && local.Status == (dados.Ativo ? "Ativo" : "Inativo");
    }
}
