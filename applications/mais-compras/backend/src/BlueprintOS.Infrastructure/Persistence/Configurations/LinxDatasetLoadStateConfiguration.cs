using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class LinxDatasetLoadStateConfiguration : IEntityTypeConfiguration<LinxDatasetLoadState>
{
    public void Configure(EntityTypeBuilder<LinxDatasetLoadState> builder)
    {
        builder.ToTable("LinxDatasetLoadState");
        // Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): chave composta — duas
        // Unidades de Negócio executando o mesmo dataset nunca compartilham estado.
        builder.HasKey(x => new { x.UnidadeNegocioId, x.Dataset });
        builder.Property(x => x.Dataset).HasMaxLength(200);
    }
}
