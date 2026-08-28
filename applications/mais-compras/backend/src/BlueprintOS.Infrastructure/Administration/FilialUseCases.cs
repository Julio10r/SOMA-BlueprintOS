using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.Infrastructure.Administration;

/// <summary>Listagem de Filiais combinando leitura real do ERP (`IFilialErpReader`) com os metadados locais
/// já existentes (O1.7). Join em memória por <c>CodigoCliFor</c>: paginação real do ERP fica fora do escopo
/// desta sprint (mesmo padrão simplificado que a tela de Perfis/Usuários usa hoje — sem paginação de UI);
/// lê um lote amplo (até 5000) o suficiente para as bases do SOMA_DESENV neste estágio.
///
/// Quando não existe metadado local para um código ERP retornado, o registro é tratado como Ativo por
/// padrão (decisão desta sprint, documentada no relatório final da O1.7): a ausência de qualquer
/// intervenção local nunca deve, por si, ocultar ou desativar um dado mestre do ERP.</summary>
public sealed class ListarFiliaisUseCase(IFilialErpReader reader, IFilialMetadadoRepository metadados) : IListarFiliaisUseCase
{
    private const int LimiteLeitura = 5000;

    public async Task<IReadOnlyList<FilialDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var doErp = await reader.BuscarFiliaisAsync(0, LimiteLeitura, ct);
        var locais = await metadados.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);

        return doErp
            .Select(filial => Projetar(filial, locais.GetValueOrDefault(filial.CodigoCliFor)))
            .ToArray();
    }

    private static FilialDto Projetar(FilialErpDto erp, FilialMetadado? local) => new(
        erp.CodigoCliFor,
        erp.NomeCliFor,
        erp.UnidadeNegocioErpId,
        local?.DescricaoMaisCompras,
        local?.AtivoNoMaisCompras ?? true,
        local is not null,
        local?.AtualizadoEm ?? erp.UltimaAlteracaoEm);
}

/// <summary>Cria o metadado local na primeira edição/ativação de uma Filial, ou atualiza o existente.
/// Nunca cria/edita/exclui o dado ERP — apenas confirma, via <see cref="IFilialErpReader"/>, que o código
/// informado corresponde a uma Filial real antes de persistir qualquer coisa localmente.</summary>
public sealed class AtualizarMetadadoFilialUseCase(
    IFilialErpReader reader,
    IFilialMetadadoRepository metadados,
    TimeProvider clock) : IAtualizarMetadadoFilialUseCase
{
    public async Task<ErpMetadadoResultado<FilialDto>> ExecuteAsync(
        string codigoCliFor, FilialMetadadoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var codigo = (codigoCliFor ?? string.Empty).Trim();
        var erp = await reader.BuscarPorCodigoAsync(codigo, ct);
        if (erp is null)
        {
            return ErpMetadadoResultado<FilialDto>.Erro(
                ErpMetadadoFalha.CodigoErpNaoEncontrado, "Código ERP de Filial não encontrado.");
        }

        var agora = clock.GetUtcNow();
        var local = await metadados.ObterPorCodigoErpAsync(codigo, unidadeNegocioId, ct);
        if (local is null)
        {
            local = new FilialMetadado(codigo, unidadeNegocioId, agora, input.DescricaoMaisCompras, input.AtivoNoMaisCompras);
            await metadados.AdicionarAsync(local, ct);
        }
        else
        {
            local.AtualizarDescricao(input.DescricaoMaisCompras, agora);
            if (input.AtivoNoMaisCompras) local.Ativar(agora); else local.Inativar(agora);
        }

        await metadados.SalvarAlteracoesAsync(ct);

        var dto = new FilialDto(erp.CodigoCliFor, erp.NomeCliFor, erp.UnidadeNegocioErpId,
            local.DescricaoMaisCompras, local.AtivoNoMaisCompras, true, local.AtualizadoEm);
        return ErpMetadadoResultado<FilialDto>.Ok(dto);
    }
}
