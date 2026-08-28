using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class SessaoAutenticacaoConfiguration : IEntityTypeConfiguration<SessaoAutenticacao>
{
    public void Configure(EntityTypeBuilder<SessaoAutenticacao> builder)
    {
        builder.ToTable("SessoesAutenticacao");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdentificadorHash).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.IdentificadorHash).IsUnique();
    }
}
