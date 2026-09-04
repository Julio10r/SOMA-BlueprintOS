using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>Filtro de status para a pesquisa paginada de Fornecedores (O1.x, redesenho da tela de
/// Fornecedores). "Todos" não filtra por Status.</summary>
public enum FornecedorStatusFiltro { Todos, Ativo, Inativo }

/// <summary>Campos aceitos para ordenação da pesquisa paginada. O prefixo de direção (asc/desc) é
/// resolvido pelo use case/repositório a partir do parâmetro de sort recebido na API.</summary>
public enum FornecedorOrdenacaoCampo { RazaoSocial, Cnpj, Status, CreatedAt }

public sealed record FornecedorPesquisaPaginadaResultado(IReadOnlyList<Fornecedor> Items, int TotalCount, int Page, int PageSize);

public interface IFornecedorRepository
{
    Task AdicionarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);

    /// <summary>Variante de <see cref="AdicionarAsync"/> que apenas marca a entidade como rastreada
    /// (Added) no DbContext, sem chamar SaveChangesAsync. Existe para permitir que chamadores de alto
    /// volume (ex.: sincronização em lote com o ERP) acumulem várias mudanças e persistam em batches,
    /// em vez de um round-trip ao banco por registro. A implementação padrão preserva o comportamento
    /// histórico (salva imediatamente) para qualquer implementação de <see cref="IFornecedorRepository"/>
    /// que não sobrescreva este método explicitamente.</summary>
    Task AdicionarSemSalvarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default) =>
        AdicionarAsync(fornecedor, cancellationToken);

    /// <summary>Variante de <see cref="AtualizarAsync"/> que apenas marca a entidade como Modified no
    /// DbContext, sem chamar SaveChangesAsync. Ver <see cref="AdicionarSemSalvarAsync"/>.</summary>
    Task AtualizarSemSalvarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default) =>
        AtualizarAsync(fornecedor, cancellationToken);
    Task<Fornecedor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Onda 2 (Multi-BU/Multi-ERP): identidade funcional real é (UnidadeNegocioId, Cnpj) — busca
    /// sempre escopada pela Business Unit, nunca global. O mesmo CNPJ pode existir como Fornecedores
    /// independentes em BUs diferentes.</summary>
    Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, Guid unidadeNegocioId, CancellationToken cancellationToken = default);

    /// <summary>[LEGADO/COMPATIBILIDADE — B3 Bloco 5A.9] `Fornecedor.ErpFornecedorId` deixou de ser a
    /// identidade ERP canônica com a introdução do modelo 1 CNPJ = 1 Fornecedor, N vínculos Linx
    /// (<see cref="Domain.Procurement.Suppliers.FornecedorLinxVinculo"/>, GAPs KALUNGA/PLATINUM) — o campo
    /// agora só espelha o vínculo Principal ATIVO atual, por compatibilidade com leitores existentes. NÃO
    /// usar para resolver identidade Linx em novos relacionamentos (ex.: Referências de Item Fiscal, que
    /// passaram a usar <see cref="IFornecedorLinxVinculoRepository.ObterPorErpSistemaECodigoAsync"/> —
    /// resolve por QUALQUER vínculo conhecido, não só o Principal). Mantido apenas para não quebrar
    /// consumidores existentes do campo legado; não remover sem antes confirmar ausência de uso.</summary>
    Task<Fornecedor?> ObterPorErpFornecedorIdAsync(string erpFornecedorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fornecedor>> PesquisarAsync(string termo, Guid unidadeNegocioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fornecedor>> ListarAsync(Guid unidadeNegocioId, CancellationToken cancellationToken = default);
    Task<bool> ExisteAsync(string documentoFiscal, Guid unidadeNegocioId, CancellationToken cancellationToken = default);

    /// <summary>Leitura sem rastreamento (AsNoTracking) usada quando o chamador só precisa classificar
    /// o registro (existe/não existe, houve alteração) e pode decidir depois — na mesma unit-of-work —
    /// se vai ou não persistir uma atualização. Ver requisito de otimização para grande volume
    /// (SincronizarFornecedoresErpUseCase): evita que o ChangeTracker acumule entidades lidas apenas
    /// para verificação. Quando uma atualização é de fato necessária, <see cref="AtualizarAsync"/> anexa
    /// a entidade explicitamente (via <c>DbSet.Update</c>), então o fato de ter sido lida sem
    /// rastreamento não impede a escrita.</summary>
    Task<Fornecedor?> ObterPorCnpjSemRastreamentoAsync(string cnpj, Guid unidadeNegocioId, CancellationToken cancellationToken = default);

    /// <summary>Total de Fornecedores corporativos com Status "Ativo". Usado pela guarda de segurança de
    /// inativação em massa (SincronizarFornecedoresErpUseCase) como denominador do percentual de
    /// inativação de uma execução.</summary>
    Task<int> ContarAtivosAsync(Guid unidadeNegocioId, CancellationToken cancellationToken = default);

    /// <summary>Pesquisa paginada, filtrável por status e ordenável, aplicada inteiramente no nível do
    /// IQueryable (sem materializar antes de paginar). <paramref name="termo"/> casa contra CNPJ ou
    /// RazaoSocial/NomeFantasia (Contains, case-insensitive). <paramref name="ordenarDescendente"/>
    /// inverte a direção padrão (ascendente) do campo escolhido em <paramref name="ordenarPor"/>.</summary>
    Task<FornecedorPesquisaPaginadaResultado> PesquisarPaginadoAsync(string? termo,
        FornecedorStatusFiltro status, FornecedorOrdenacaoCampo ordenarPor, bool ordenarDescendente,
        int page, int pageSize, Guid unidadeNegocioId, CancellationToken cancellationToken = default);
}
