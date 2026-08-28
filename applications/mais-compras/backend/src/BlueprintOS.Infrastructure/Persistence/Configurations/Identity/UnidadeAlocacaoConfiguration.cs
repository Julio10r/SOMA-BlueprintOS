using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class UnidadeAlocacaoConfiguration : IEntityTypeConfiguration<UnidadeAlocacao>
{
    public void Configure(EntityTypeBuilder<UnidadeAlocacao> builder)
    {
        builder.ToTable("UnidadesAlocacao");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Descricao).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.Nome }).IsUnique();
    }
}
