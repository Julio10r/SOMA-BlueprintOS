using BlueprintOS.Domain.Identity.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class RawLinxUnidadeMedidaRegistroConfiguration : IEntityTypeConfiguration<RawLinxUnidadeMedidaRegistro>
{
    public void Configure(EntityTypeBuilder<RawLinxUnidadeMedidaRegistro> builder)
    {
        builder.ToTable("RAW_LinxUnidadesMedidaSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).HasMaxLength(5).IsRequired();
        builder.Property(x => x.DescricaoErp).HasMaxLength(40);
        builder.Property(x => x.UltimaAlteracao).HasColumnType("datetime");
        builder.HasIndex(x => x.CodigoErp);
    }
}
