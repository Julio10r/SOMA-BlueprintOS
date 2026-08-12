using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class FornecedorCnpjConsultaHistoricoRepository(BlueprintOSDbContext context) : IFornecedorCnpjConsultaHistoricoRepository
{
    public async Task AdicionarAsync(FornecedorCnpjConsultaHistorico consulta, CancellationToken cancellationToken = default)
    {
        await context.Set<FornecedorCnpjConsultaHistorico>().AddAsync(consulta, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ExpurgarPayloadBrutoExpiradoAsync(DateTimeOffset referenciaUtc, CancellationToken cancellationToken = default)
    {
        var corte = referenciaUtc.AddDays(-FornecedorCnpjConsultaHistorico.RetencaoPayloadBrutoDias);

        // ExecuteUpdateAsync (EF Core batch update): a expiração vira uma única instrução de UPDATE no
        // banco, sem carregar snapshots em memória e sem desserializar JSON para decidir elegibilidade
        // (a decisão é puramente por DataConsulta, já indexada). O provider InMemory (usado nos testes
        // unitários) não traduz ExecuteUpdateAsync — nesse caso caímos para o caminho baseado em
        // rastreamento de entidades, funcionalmente idêntico (mesmo filtro, mesma idempotência, apenas
        // sem o ganho de performance de UPDATE em lote), preservando o mesmo contrato/comportamento em
        // todos os provedores suportados pelo projeto.
        if (context.Database.IsRelational())
        {
            return await context.Set<FornecedorCnpjConsultaHistorico>()
                .Where(x => x.DataConsulta < corte && x.PayloadBrutoJson != null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.PayloadBrutoJson, x => null), cancellationToken);
        }

        var elegiveis = await context.Set<FornecedorCnpjConsultaHistorico>()
            .Where(x => x.DataConsulta < corte && x.PayloadBrutoJson != null)
            .ToListAsync(cancellationToken);
        foreach (var registro in elegiveis)
        {
            registro.ExpirarPayloadBruto();
        }
        if (elegiveis.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        return elegiveis.Count;
    }
}
