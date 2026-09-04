using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class RawLinxFornecedorSnapshotRegistroConfiguration : IEntityTypeConfiguration<RawLinxFornecedorSnapshotRegistro>
{
    public void Configure(EntityTypeBuilder<RawLinxFornecedorSnapshotRegistro> builder)
    {
        builder.ToTable("RAW_LinxFornecedoresSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoFornecedor).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Clifor).HasMaxLength(20);
        builder.Property(x => x.CnpjCpf).HasMaxLength(20);
        builder.Property(x => x.RazaoSocial).HasMaxLength(400);
        builder.Property(x => x.NomeFantasia).HasMaxLength(400);
        builder.Property(x => x.TipoPessoa).HasMaxLength(5);
        builder.Property(x => x.UltimaAlteracao).HasColumnType("datetime");
        builder.HasIndex(x => x.CnpjCpf);
    }
}
