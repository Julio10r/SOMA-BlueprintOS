using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class RawLinxFornecedorSnapshotExecucaoConfiguration : IEntityTypeConfiguration<RawLinxFornecedorSnapshotExecucao>
{
    public void Configure(EntityTypeBuilder<RawLinxFornecedorSnapshotExecucao> builder)
    {
        builder.ToTable("RAW_LinxFornecedoresSnapshotExecucoes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Dataset).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Modo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.IniciadoEm).IsRequired();
        builder.Property(x => x.IsolamentoUtilizado).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Erro).HasMaxLength(4000);
        builder.Property(x => x.ReconciliacaoStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.Dataset, x.IniciadoEm });
        builder.HasIndex(x => new { x.Dataset, x.Modo, x.ReconciliacaoStatus });
    }
}
