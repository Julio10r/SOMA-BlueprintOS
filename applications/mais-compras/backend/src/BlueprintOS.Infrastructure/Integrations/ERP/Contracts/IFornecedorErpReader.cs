using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

public interface IFornecedorErpReader
{
    Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(
        int limite,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

/// <summary><c>InativoCadastroCliFor</c> — B3 Bloco 5A.9: `CADASTRO_CLI_FOR.INATIVO`, decisão do Product
/// Owner de que essa tabela é master do cadastro de pessoa/fornecedor no Linx — um vínculo só é Ativo
/// quando NEM esta coluna NEM `Dados.Ativo` (`FORNECEDORES.INATIVO`) o marcam inativo. Comprovado real
/// (Bloco 5A.8): 2.763 registros divergem entre as duas tabelas.</summary>
public sealed record FornecedorErpIntegracaoDto(
    string ErpFornecedorId,
    string ErpSistema,
    FornecedorCanonico Dados,
    DateTimeOffset? UltimaAlteracaoEm,
    bool InativoCadastroCliFor);
