namespace BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

/// <summary>Leitura real (somente leitura) de Contas Contábeis do ERP `SOMA_DESENV` (B3 — Bloco 1,
/// `CTB_CONTA_PLANO`). Mesmo padrão de <c>IFilialErpReader</c>/<c>ICentroCustoErpReader</c>: o ERP é fonte
/// canônica e imutável — Conta Contábil é cadastro de apoio originado do Linx (Discovery B3 homologado,
/// `ContratoFuncionalPreliminar-B3-ItemFiscal.md` §2); o +Compras nunca cria/edita/exclui o dado mestre,
/// apenas lê para exibir e correlacionar com metadados locais.</summary>
public interface IContaContabilErpReader
{
    Task<IReadOnlyList<ContaContabilErpDto>> BuscarContasContabeisAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<ContaContabilErpDto?> BuscarPorCodigoAsync(string codigoErp, CancellationToken cancellationToken = default);
}

public sealed record ContaContabilErpDto(
    string CodigoErp,
    string DescricaoErp,
    bool InativaNoErp,
    DateTimeOffset? UltimaAlteracaoEm);
