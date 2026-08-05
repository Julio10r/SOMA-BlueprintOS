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
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TempoExecucaoMs).IsRequired();
        builder.HasMany(x => x.Erros).WithOne().HasForeignKey(x => x.SincronizacaoFornecedorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.BusinessUnit, x.SistemaOrigem, x.DataInicio });
        builder.HasIndex(x => x.Status);
    }
}
