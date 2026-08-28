using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class ConfiguracaoNotificacaoConfiguration : IEntityTypeConfiguration<ConfiguracaoNotificacao>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoNotificacao> builder)
    {
        builder.ToTable("ConfiguracoesNotificacao");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmailAtivado).IsRequired();
        builder.Property(x => x.EmailRemetente).HasMaxLength(320);
        builder.Property(x => x.NomeRemetente).HasMaxLength(200);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Relação 1:1 — uma Unidade de Negócio tem no máximo uma Configuração de Notificações.
        builder.HasIndex(x => x.UnidadeNegocioId).IsUnique();
    }
}
