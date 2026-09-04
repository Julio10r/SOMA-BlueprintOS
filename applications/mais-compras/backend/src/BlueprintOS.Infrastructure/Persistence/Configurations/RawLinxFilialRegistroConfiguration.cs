using BlueprintOS.Domain.Identity.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class RawLinxFilialRegistroConfiguration : IEntityTypeConfiguration<RawLinxFilialRegistro>
{
    public void Configure(EntityTypeBuilder<RawLinxFilialRegistro> builder)
    {
        builder.ToTable("RAW_LinxFiliaisSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).HasMaxLength(25).IsRequired();
        builder.Property(x => x.DescricaoErp).HasMaxLength(200);
        builder.Property(x => x.UltimaAlteracao).HasColumnType("datetime");
        builder.HasIndex(x => x.CodigoErp);
    }
}
