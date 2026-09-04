using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

/// <summary>B3 — Bloco 1: metadados locais +Compras de Conta Contábil. Sem FK para a Conta Contábil em
/// si: não existe tabela local do dado mestre (ele permanece só no ERP) —
/// <see cref="ContaContabilMetadado.CodigoErp"/> é apenas uma chave de correlação em texto.</summary>
public sealed class ContaContabilMetadadoConfiguration : IEntityTypeConfiguration<ContaContabilMetadado>
{
    public void Configure(EntityTypeBuilder<ContaContabilMetadado> builder)
    {
        builder.ToTable("ContasContabeisMetadados");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DescricaoMaisCompras).HasMaxLength(400);
        builder.Property(x => x.AtivoNoMaisCompras).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner registrada em
        // applications/mais-compras/docs/cadernos/Onda-2.md): único por (UnidadeNegocioId, CodigoErp), não
        // mais globalmente — o mesmo código ERP pode existir em BUs diferentes como contextos
        // independentes (ex.: Grupo Soma/código 001 e Reserva/código 001 nunca compartilham metadado).
        // Substitui a decisão anterior de unicidade global (preservada apenas historicamente nesta nota).
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.CodigoErp })
            .IsUnique()
            .HasDatabaseName("IX_ContasContabeisMetadados_UnidadeNegocioId_CodigoErp");
    }
}
