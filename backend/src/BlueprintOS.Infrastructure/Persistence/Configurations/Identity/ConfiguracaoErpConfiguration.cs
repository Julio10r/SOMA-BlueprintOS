using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class ConfiguracaoErpConfiguration : IEntityTypeConfiguration<ConfiguracaoErp>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoErp> builder)
    {
        builder.ToTable("ConfiguracoesErp");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SistemaErp).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ParametrosConexaoProtegidos).HasMaxLength(4000);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Relação 1:1 — uma Unidade de Negócio tem no máximo uma Configuração de ERP.
        builder.HasIndex(x => x.UnidadeNegocioId).IsUnique();
    }
}
