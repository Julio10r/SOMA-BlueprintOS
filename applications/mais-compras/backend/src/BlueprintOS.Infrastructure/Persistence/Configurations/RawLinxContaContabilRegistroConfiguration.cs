using BlueprintOS.Domain.Identity.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class RawLinxContaContabilRegistroConfiguration : IEntityTypeConfiguration<RawLinxContaContabilRegistro>
{
    public void Configure(EntityTypeBuilder<RawLinxContaContabilRegistro> builder)
    {
        builder.ToTable("RAW_LinxContasContabeisSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DescricaoErp).HasMaxLength(40);
        builder.Property(x => x.UltimaAlteracao).HasColumnType("datetime");
        builder.HasIndex(x => x.CodigoErp);
    }
}
