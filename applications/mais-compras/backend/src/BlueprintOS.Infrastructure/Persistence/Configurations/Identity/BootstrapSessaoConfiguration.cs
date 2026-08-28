using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class BootstrapSessaoConfiguration : IEntityTypeConfiguration<BootstrapSessao>
{
    public void Configure(EntityTypeBuilder<BootstrapSessao> builder)
    {
        builder.ToTable("BootstrapSessoes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmailCandidato).IsRequired().HasMaxLength(320);
        builder.Property(x => x.IdentificadorHash).IsRequired().HasMaxLength(100);

        // Mesmo padrão de índice único já usado para SessaoAutenticacao.IdentificadorHash (Work Order
        // O1.4.3, seção 8).
        builder.HasIndex(x => x.IdentificadorHash).IsUnique();
    }
}
