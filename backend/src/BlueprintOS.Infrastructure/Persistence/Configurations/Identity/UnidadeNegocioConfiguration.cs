using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class UnidadeNegocioConfiguration : IEntityTypeConfiguration<UnidadeNegocio>
{
    public void Configure(EntityTypeBuilder<UnidadeNegocio> builder)
    {
        builder.ToTable("UnidadesNegocio");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
