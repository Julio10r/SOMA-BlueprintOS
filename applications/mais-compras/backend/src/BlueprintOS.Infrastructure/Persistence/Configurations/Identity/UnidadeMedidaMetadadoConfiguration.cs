using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

/// <summary>B3 — Bloco 2: metadados locais +Compras de Unidade de Medida. Sem FK para a Unidade em si:
/// não existe tabela local do dado mestre (ele permanece só no ERP) —
/// <see cref="UnidadeMedidaMetadado.CodigoErp"/> é apenas uma chave de correlação em texto.</summary>
public sealed class UnidadeMedidaMetadadoConfiguration : IEntityTypeConfiguration<UnidadeMedidaMetadado>
{
    public void Configure(EntityTypeBuilder<UnidadeMedidaMetadado> builder)
    {
        builder.ToTable("UnidadesMedidaMetadados");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DescricaoMaisCompras).HasMaxLength(400);
        builder.Property(x => x.AtivoNoMaisCompras).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner registrada em
        // applications/mais-compras/docs/cadernos/Onda-2.md): único por (UnidadeNegocioId, CodigoErp), não
        // mais globalmente — mesma normalização de ContaContabilMetadadoConfiguration/
        // CentroCustoMetadadoConfiguration/FilialMetadadoConfiguration.
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.CodigoErp })
            .IsUnique()
            .HasDatabaseName("IX_UnidadesMedidaMetadados_UnidadeNegocioId_CodigoErp");
    }
}
