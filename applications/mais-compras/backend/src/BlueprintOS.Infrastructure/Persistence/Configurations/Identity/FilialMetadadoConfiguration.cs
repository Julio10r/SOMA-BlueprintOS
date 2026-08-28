using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

/// <summary>O1.7 — metadados locais +Compras de Filial. Sem FK para a Filial em si: não existe tabela
/// local do dado mestre (ele permanece só no ERP, ADR-0020 item 3) — <see cref="FilialMetadado.CodigoErp"/>
/// é apenas uma chave de correlação em texto.</summary>
public sealed class FilialMetadadoConfiguration : IEntityTypeConfiguration<FilialMetadado>
{
    public void Configure(EntityTypeBuilder<FilialMetadado> builder)
    {
        builder.ToTable("FiliaisMetadados");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DescricaoMaisCompras).HasMaxLength(400);
        builder.Property(x => x.AtivoNoMaisCompras).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        builder.HasIndex(x => new { x.UnidadeNegocioId, x.CodigoErp })
            .IsUnique()
            .HasDatabaseName("IX_FiliaisMetadados_UnidadeNegocioId_CodigoErp");
    }
}
