using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class IdentityProviderConfiguration : IEntityTypeConfiguration<IdentityProvider>
{
    public void Configure(EntityTypeBuilder<IdentityProvider> builder)
    {
        builder.ToTable("IdentityProviders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Tipo).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DominiosAutorizadosCsv).IsRequired().HasMaxLength(2000).HasDefaultValue(string.Empty);
        builder.Property(x => x.ParametrosProtegidos).HasMaxLength(4000);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();
        builder.Ignore(x => x.DominiosAutorizados);
        builder.HasIndex(x => x.UnidadeNegocioId);
    }
}
