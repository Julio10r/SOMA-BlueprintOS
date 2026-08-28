using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class ParametroConfiguration : IEntityTypeConfiguration<Parametro>
{
    public void Configure(EntityTypeBuilder<Parametro> builder)
    {
        builder.ToTable("Parametros");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Chave).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Valor).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Descricao).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // UnidadeNegocioId nulo = parâmetro global — único por (Chave, UnidadeNegocioId). SQL Server trata
        // NULL como distinto em índices únicos, então o índice combinado sozinho NÃO impediria duas
        // linhas globais com a mesma Chave — o filtro explícito abaixo cobre esse caso.
        builder.HasIndex(x => new { x.Chave, x.UnidadeNegocioId })
            .IsUnique()
            .HasFilter("[UnidadeNegocioId] IS NOT NULL");
        builder.HasIndex(x => x.Chave)
            .IsUnique()
            .HasDatabaseName("IX_Parametros_Chave_Global")
            .HasFilter("[UnidadeNegocioId] IS NULL");
    }
}
