using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class RawLinxFornecedorDominioErpRegistroConfiguration : IEntityTypeConfiguration<RawLinxFornecedorDominioErpRegistro>
{
    public void Configure(EntityTypeBuilder<RawLinxFornecedorDominioErpRegistro> builder)
    {
        builder.ToTable("RAW_LinxFornecedorDominiosSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TipoDominio).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CodigoErp).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Descricao).HasMaxLength(100);
        builder.Property(x => x.UltimaAlteracao).HasColumnType("datetime");
        builder.HasIndex(x => new { x.TipoDominio, x.CodigoErp });
    }
}
