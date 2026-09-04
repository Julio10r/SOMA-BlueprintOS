using BlueprintOS.Domain.Identity.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class RawLinxItemFiscalRegistroConfiguration : IEntityTypeConfiguration<RawLinxItemFiscalRegistro>
{
    public void Configure(EntityTypeBuilder<RawLinxItemFiscalRegistro> builder)
    {
        builder.ToTable("RAW_LinxItensFiscaisSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Descricao).HasMaxLength(80).IsRequired();
        builder.Property(x => x.UnidadeErp).HasMaxLength(5);
        builder.Property(x => x.ContaContabilErp).HasMaxLength(20);
        builder.Property(x => x.UltimaAlteracao).HasColumnType("datetime");
        builder.HasIndex(x => x.CodigoErp);
    }
}
