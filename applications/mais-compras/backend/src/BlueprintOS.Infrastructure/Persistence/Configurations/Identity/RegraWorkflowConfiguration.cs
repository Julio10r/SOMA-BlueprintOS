using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class RegraWorkflowConfiguration : IEntityTypeConfiguration<RegraWorkflow>
{
    public void Configure(EntityTypeBuilder<RegraWorkflow> builder)
    {
        builder.ToTable("RegrasWorkflow");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(200);
        builder.Property(x => x.TipoProcesso).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Ordem).IsRequired();
        builder.Property(x => x.Ativo).IsRequired();
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();
        builder.HasIndex(x => x.UnidadeNegocioId);
    }
}
