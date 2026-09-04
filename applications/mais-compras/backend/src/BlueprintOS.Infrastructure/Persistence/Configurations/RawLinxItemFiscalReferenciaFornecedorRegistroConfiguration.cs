using BlueprintOS.Domain.Identity.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class RawLinxItemFiscalReferenciaFornecedorRegistroConfiguration : IEntityTypeConfiguration<RawLinxItemFiscalReferenciaFornecedorRegistro>
{
    public void Configure(EntityTypeBuilder<RawLinxItemFiscalReferenciaFornecedorRegistro> builder)
    {
        builder.ToTable("RAW_LinxItensFiscaisReferenciasFornecedorSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoItem).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CodigoItemFornecedor).HasMaxLength(25).IsRequired();
        builder.Property(x => x.ErpFornecedorId).HasMaxLength(50);
    }
}
