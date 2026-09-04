using BlueprintOS.Domain.Identity.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class RawLinxCentroCustoRegistroConfiguration : IEntityTypeConfiguration<RawLinxCentroCustoRegistro>
{
    public void Configure(EntityTypeBuilder<RawLinxCentroCustoRegistro> builder)
    {
        builder.ToTable("RAW_LinxCentrosCustoSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).HasMaxLength(15).IsRequired();
        builder.Property(x => x.DescricaoErp).HasMaxLength(40);
        builder.Property(x => x.UltimaAlteracao).HasColumnType("datetime");
        builder.HasIndex(x => x.CodigoErp);
    }
}
