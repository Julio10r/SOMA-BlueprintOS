using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("FeatureFlags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Descricao).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.HasIndex(x => x.Nome).IsUnique();
    }
}

public sealed class FeatureFlagUnidadeNegocioConfiguration : IEntityTypeConfiguration<FeatureFlagUnidadeNegocio>
{
    public void Configure(EntityTypeBuilder<FeatureFlagUnidadeNegocio> builder)
    {
        builder.ToTable("FeatureFlagsUnidadesNegocio");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AtualizadoEm).IsRequired();
        builder.HasIndex(x => new { x.FeatureFlagId, x.UnidadeNegocioId }).IsUnique();
    }
}
