using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class SincronizacaoFornecedorConfiguration : IEntityTypeConfiguration<SincronizacaoFornecedor>
{
    public void Configure(EntityTypeBuilder<SincronizacaoFornecedor> builder)
    {
        builder.ToTable("SincronizacoesFornecedores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SistemaOrigem).HasMaxLength(80).IsRequired();
        builder.Property(x => x.BusinessUnit).HasMaxLength(80).IsRequired();
        builder.Property(x => x.DataInicio).IsRequired();
        // B3 — Bloco 5A.9: 20 caracteres era insuficiente mesmo para um status pré-existente
        // ("AbortadoInativacaoAnormal", 25 chars) — bug latente nunca exercitado contra SQL Server real
        // (apenas InMemory nos testes, que não impõe o limite de coluna). Os novos status terminais do
        // GAP KALUNGA ("AbortadoRecuperacaoAdministrativa", 34 chars) tornaram o erro real e visível.
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TempoExecucaoMs).IsRequired();
        builder.Property(x => x.UnidadeNegocioId).IsRequired();
        builder.Property(x => x.JustificativaEncerramento).HasMaxLength(1000);
        builder.Property(x => x.UsuarioRecuperacaoId);
        builder.HasMany(x => x.Erros).WithOne().HasForeignKey(x => x.SincronizacaoFornecedorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.BusinessUnit, x.SistemaOrigem, x.DataInicio });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.UnidadeNegocioId);
    }
}
