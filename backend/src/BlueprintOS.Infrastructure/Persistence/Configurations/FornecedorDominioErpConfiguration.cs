using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class FornecedorDominioErpConfiguration : IEntityTypeConfiguration<FornecedorDominioErp>
{
    public void Configure(EntityTypeBuilder<FornecedorDominioErp> builder)
    {
        builder.ToTable("FornecedoresDominiosErp");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Tipo).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CodigoERP).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BusinessUnit).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ErpSistema).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.UltimaSincronizacaoEm).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => new { x.Tipo, x.BusinessUnit, x.ErpSistema, x.CodigoERP }).IsUnique();
    }
}
