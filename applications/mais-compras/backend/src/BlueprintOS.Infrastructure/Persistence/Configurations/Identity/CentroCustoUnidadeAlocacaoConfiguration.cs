using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class CentroCustoUnidadeAlocacaoConfiguration : IEntityTypeConfiguration<CentroCustoUnidadeAlocacao>
{
    public void Configure(EntityTypeBuilder<CentroCustoUnidadeAlocacao> builder)
    {
        builder.ToTable("CentrosCustoUnidadesAlocacao");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CriadoEm).IsRequired();

        builder.HasIndex(x => new { x.CentroCustoMetadadoId, x.UnidadeAlocacaoId }).IsUnique();

        // No máximo um vínculo "padrão" por Centro de Custo — índice único filtrado (SQL Server), mesma
        // técnica de índice condicional já usada no projeto para invariantes de unicidade parcial.
        builder.HasIndex(x => x.CentroCustoMetadadoId)
            .IsUnique()
            .HasFilter("[Padrao] = 1")
            .HasDatabaseName("IX_CentrosCustoUnidadesAlocacao_CentroCustoMetadadoId_Padrao");

        builder.HasOne<CentroCustoMetadado>()
            .WithMany()
            .HasForeignKey(x => x.CentroCustoMetadadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnidadeAlocacao>()
            .WithMany()
            .HasForeignKey(x => x.UnidadeAlocacaoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
