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

        // Único GLOBALMENTE por CodigoErp (mesma decisão de ContaContabilMetadadoConfiguration/
        // CentroCustoMetadadoConfiguration): Unidade de Medida é cadastro de apoio compartilhado entre
        // Unidades de Negócio, não específico de uma BU.
        builder.HasIndex(x => x.CodigoErp)
            .IsUnique()
            .HasDatabaseName("IX_UnidadesMedidaMetadados_CodigoErp");
    }
}
