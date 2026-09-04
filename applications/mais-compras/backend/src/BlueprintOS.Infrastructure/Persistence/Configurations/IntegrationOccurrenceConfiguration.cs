using BlueprintOS.Domain.Integrations.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class IntegrationOccurrenceConfiguration : IEntityTypeConfiguration<IntegrationOccurrence>
{
    public void Configure(EntityTypeBuilder<IntegrationOccurrence> builder)
    {
        builder.ToTable("IntegrationOccurrences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnidadeNegocioId).IsRequired();
        builder.Property(x => x.Dataset).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Stage).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.OriginRecordKey).HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ContextoTecnico).HasMaxLength(2000);

        // Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): UnidadeNegocioId compõe o
        // dedupe — preserva ExecutionId+Dataset+Stage+Code+OriginRecordKey do desenho original, apenas
        // acrescentando a dimensão de BU para que duas Unidades de Negócio nunca colidam.
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.ExecutionId, x.Dataset, x.Stage, x.Code, x.OriginRecordKey })
            .IsUnique()
            .HasDatabaseName("IX_IntegrationOccurrences_Dedup");
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.Dataset, x.Status });
        builder.HasIndex(x => x.ExecutionId);
    }
}
