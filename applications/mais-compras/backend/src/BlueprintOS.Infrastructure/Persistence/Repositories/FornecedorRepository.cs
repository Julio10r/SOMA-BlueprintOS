using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class FornecedorRepository(BlueprintOSDbContext context) : IFornecedorRepository
{
    public async Task AdicionarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    {
        await context.Fornecedores.AddAsync(fornecedor, cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Gate de homologação de Fornecedores (2026-09-01): corrida real entre duas requisições
            // simultâneas criando o mesmo CNPJ/CPF (índice único de Cnpj_Cpf) — traduzida para o tipo
            // agnóstico de EF Core/SQL Server já usado pelos demais repositórios (UsuarioRepository,
            // CentroCustoMetadadoRepository, etc.). O chamador (CadastrarFornecedorUseCase) decide
            // convergir para o registro já criado pela requisição concorrente, em vez de falhar.
            throw new DuplicateRecordException("Documento fiscal já foi cadastrado por outra requisição concorrente.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
    public async Task AtualizarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    {
        // Se outra instância com a mesma PK já estiver rastreada neste DbContext (ex.: leituras via
        // ObterPorCnpjSemRastreamentoAsync/AsNoTracking convivendo, na mesma unit-of-work, com uma
        // entidade tracked por uma chamada anterior a este mesmo repositório), DbSet.Update lançaria
        // "another instance with the same key value is already being tracked". Em vez de anexar uma
        // segunda instância, copiamos os valores atuais para a instância já rastreada.
        var rastreado = context.ChangeTracker.Entries<Fornecedor>().FirstOrDefault(e => e.Entity.Id == fornecedor.Id);
        if (rastreado is not null && !ReferenceEquals(rastreado.Entity, fornecedor))
        {
            rastreado.CurrentValues.SetValues(fornecedor);
        }
        else
        {
            context.Fornecedores.Update(fornecedor);
        }
        await context.SaveChangesAsync(cancellationToken);
    }
    public Task<Fornecedor?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Fornecedores.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, Guid unidadeNegocioId, CancellationToken cancellationToken = default) =>
        context.Fornecedores.SingleOrDefaultAsync(x => x.Cnpj_Cpf == cnpj && x.UnidadeNegocioId == unidadeNegocioId, cancellationToken);
    public Task<Fornecedor?> ObterPorErpFornecedorIdAsync(string erpFornecedorId, CancellationToken cancellationToken = default) =>
        context.Fornecedores.SingleOrDefaultAsync(x => x.ErpFornecedorId == erpFornecedorId, cancellationToken);
    public async Task<IReadOnlyList<Fornecedor>> PesquisarAsync(string termo, Guid unidadeNegocioId, CancellationToken cancellationToken = default) =>
        await context.Fornecedores.AsNoTracking().Where(x => x.UnidadeNegocioId == unidadeNegocioId &&
            (x.RazaoSocial.Contains(termo) || x.Cnpj_Cpf.Contains(termo))).OrderBy(x => x.RazaoSocial).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Fornecedor>> ListarAsync(Guid unidadeNegocioId, CancellationToken cancellationToken = default) =>
        await context.Fornecedores.AsNoTracking().Where(x => x.UnidadeNegocioId == unidadeNegocioId).OrderBy(x => x.RazaoSocial).ToListAsync(cancellationToken);
    public Task<bool> ExisteAsync(string cnpj, Guid unidadeNegocioId, CancellationToken cancellationToken = default) =>
        context.Fornecedores.AnyAsync(x => x.Cnpj_Cpf == cnpj && x.UnidadeNegocioId == unidadeNegocioId, cancellationToken);
    public Task<Fornecedor?> ObterPorCnpjSemRastreamentoAsync(string cnpj, Guid unidadeNegocioId, CancellationToken cancellationToken = default) =>
        context.Fornecedores.AsNoTracking().SingleOrDefaultAsync(x => x.Cnpj_Cpf == cnpj && x.UnidadeNegocioId == unidadeNegocioId, cancellationToken);
    public Task<int> ContarAtivosAsync(Guid unidadeNegocioId, CancellationToken cancellationToken = default) =>
        context.Fornecedores.AsNoTracking().CountAsync(x => x.Status == "Ativo" && x.UnidadeNegocioId == unidadeNegocioId, cancellationToken);

    public async Task<FornecedorPesquisaPaginadaResultado> PesquisarPaginadoAsync(string? termo,
        FornecedorStatusFiltro status, FornecedorOrdenacaoCampo ordenarPor, bool ordenarDescendente,
        int page, int pageSize, Guid unidadeNegocioId, CancellationToken cancellationToken = default)
    {
        var pagina = page < 1 ? 1 : page;
        var tamanho = pageSize < 1 ? 20 : pageSize;

        IQueryable<Fornecedor> query = context.Fornecedores.AsNoTracking().Where(x => x.UnidadeNegocioId == unidadeNegocioId);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(x => x.RazaoSocial.Contains(termo) || x.Cnpj_Cpf.Contains(termo)
                || (x.NomeFantasia != null && x.NomeFantasia.Contains(termo)));
        }

        query = status switch
        {
            FornecedorStatusFiltro.Ativo => query.Where(x => x.Status == "Ativo"),
            FornecedorStatusFiltro.Inativo => query.Where(x => x.Status == "Inativo"),
            _ => query,
        };

        query = ordenarPor switch
        {
            FornecedorOrdenacaoCampo.Cnpj => ordenarDescendente ? query.OrderByDescending(x => x.Cnpj_Cpf) : query.OrderBy(x => x.Cnpj_Cpf),
            FornecedorOrdenacaoCampo.Status => ordenarDescendente ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            FornecedorOrdenacaoCampo.CreatedAt => ordenarDescendente ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => ordenarDescendente ? query.OrderByDescending(x => x.RazaoSocial) : query.OrderBy(x => x.RazaoSocial),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pagina - 1) * tamanho).Take(tamanho).ToListAsync(cancellationToken);
        return new FornecedorPesquisaPaginadaResultado(items, totalCount, pagina, tamanho);
    }
}
