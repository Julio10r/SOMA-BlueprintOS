using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.Infrastructure.Administration;

/// <summary>Listagem de Centros de Custo combinando leitura real do ERP (`ICentroCustoErpReader`) com os
/// metadados locais já existentes (O1.7). Mesmo padrão/mesma decisão de "ativo por padrão sem metadado
/// local" de <c>ListarFiliaisUseCase</c> — ver relatório final da O1.7.</summary>
public sealed class ListarCentrosCustoUseCase(ICentroCustoErpReader reader, ICentroCustoMetadadoRepository metadados) : IListarCentrosCustoUseCase
{
    private const int LimiteLeitura = 5000;

    public async Task<IReadOnlyList<CentroCustoDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var doErp = await reader.BuscarCentrosCustoAsync(0, LimiteLeitura, ct);
        var locais = await metadados.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);

        return doErp
            .Select(centro => Projetar(centro, locais.GetValueOrDefault(centro.CodigoErp)))
            .ToArray();
    }

    private static CentroCustoDto Projetar(CentroCustoErpDto erp, CentroCustoMetadado? local) => new(
        erp.CodigoErp,
        erp.DescricaoErp,
        local?.DescricaoMaisCompras,
        local?.AtivoNoMaisCompras ?? true,
        local is not null,
        local?.AtualizadoEm ?? erp.UltimaAlteracaoEm);
}

/// <summary>Cria o metadado local na primeira edição/ativação de um Centro de Custo, ou atualiza o
/// existente. Nunca cria/edita/exclui o dado ERP — apenas confirma, via <see cref="ICentroCustoErpReader"/>,
/// que o código informado corresponde a um Centro de Custo real antes de persistir qualquer coisa
/// localmente.</summary>
public sealed class AtualizarMetadadoCentroCustoUseCase(
    ICentroCustoErpReader reader,
    ICentroCustoMetadadoRepository metadados,
    TimeProvider clock) : IAtualizarMetadadoCentroCustoUseCase
{
    public async Task<ErpMetadadoResultado<CentroCustoDto>> ExecuteAsync(
        string codigoErp, CentroCustoMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var codigo = (codigoErp ?? string.Empty).Trim();
        var erp = await reader.BuscarPorCodigoAsync(codigo, ct);
        if (erp is null)
        {
            return ErpMetadadoResultado<CentroCustoDto>.Erro(
                ErpMetadadoFalha.CodigoErpNaoEncontrado, "Código ERP de Centro de Custo não encontrado.");
        }

        var agora = clock.GetUtcNow();
        var local = await metadados.ObterPorCodigoErpAsync(codigo, unidadeNegocioId, ct);
        if (local is null)
        {
            // CentrosCustoMetadados.CodigoErp é globalmente único (ver CentroCustoMetadadoConfiguration):
            // um Centro de Custo só pode estar ancorado a UMA Unidade de Negócio por vez (mesma regra do
            // vínculo Usuário×Centro de Custo, O1.6-L2). Sem esta verificação, a criação abaixo violaria
            // o índice único e vazaria um DbUpdateException não tratado para o cliente.
            var ancoraExistente = await metadados.ObterPorCodigoErpGlobalAsync(codigo, ct);
            if (ancoraExistente is not null && ancoraExistente.UnidadeNegocioId != unidadeNegocioId)
            {
                return ErpMetadadoResultado<CentroCustoDto>.Erro(
                    ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio,
                    "Este Centro de Custo já está ancorado a outra Unidade de Negócio.");
            }

            local = new CentroCustoMetadado(codigo, unidadeNegocioId, agora, input.DescricaoMaisCompras, input.AtivoNoMaisCompras);
            await metadados.AdicionarAsync(local, ct);
        }
        else
        {
            local.AtualizarDescricao(input.DescricaoMaisCompras, agora);
            if (input.AtivoNoMaisCompras) local.Ativar(agora); else local.Inativar(agora);
        }

        try
        {
            await metadados.SalvarAlteracoesAsync(ct);
        }
        catch (DuplicateRecordException)
        {
            return ErpMetadadoResultado<CentroCustoDto>.Erro(
                ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio,
                "Este Centro de Custo foi ancorado por outra requisição concorrente em outra Unidade de Negócio.");
        }

        var dto = new CentroCustoDto(erp.CodigoErp, erp.DescricaoErp,
            local.DescricaoMaisCompras, local.AtivoNoMaisCompras, true, local.AtualizadoEm);
        return ErpMetadadoResultado<CentroCustoDto>.Ok(dto);
    }
}
