using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class FornecedorRepository(BlueprintOSDbContext context) : IFornecedorRepository
{
    public async Task AdicionarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    { await context.Fornecedores.AddAsync(fornecedor, cancellationToken); await context.SaveChangesAsync(cancellationToken); }
    public async Task AtualizarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default)
    { context.Fornecedores.Update(fornecedor); await context.SaveChangesAsync(cancellationToken); }
    public Task<Fornecedor?> ObterPorIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        context.Fornecedores.SingleOrDefaultAsync(x => x.Id == id && x.TemporaryUserId == userId, cancellationToken);
    public Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, Guid userId, CancellationToken cancellationToken = default) =>
        context.Fornecedores.SingleOrDefaultAsync(x => x.Cnpj_Cpf == cnpj && x.TemporaryUserId == userId, cancellationToken);
    public async Task<IReadOnlyList<Fornecedor>> PesquisarAsync(string termo, Guid userId, CancellationToken cancellationToken = default) =>
        await context.Fornecedores.AsNoTracking().Where(x => x.TemporaryUserId == userId &&
            (x.RazaoSocial.Contains(termo) || x.Cnpj_Cpf.Contains(termo))).OrderBy(x => x.RazaoSocial).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Fornecedor>> ListarAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Fornecedores.AsNoTracking().Where(x => x.TemporaryUserId == userId).OrderBy(x => x.RazaoSocial).ToListAsync(cancellationToken);
    public Task<bool> ExisteAsync(string cnpj, CancellationToken cancellationToken = default) =>
        context.Fornecedores.AnyAsync(x => x.Cnpj_Cpf == cnpj, cancellationToken);
}
