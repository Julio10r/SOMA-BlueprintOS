using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class CodigoVerificacaoOtpConfiguration : IEntityTypeConfiguration<CodigoVerificacaoOtp>
{
    public void Configure(EntityTypeBuilder<CodigoVerificacaoOtp> builder)
    {
        builder.ToTable("CodigosVerificacaoOtp");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Hash).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Salt).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.EmailCandidato).HasMaxLength(320);
        builder.HasIndex(x => new { x.UsuarioId, x.Status });

        // Invariante de dados (O1.4.2.1, Achado B/reenvio): no máximo um código Pendente por usuário,
        // aplicado pelo próprio banco — nenhuma corrida de aplicação pode criar dois códigos válidos
        // simultâneos para o mesmo usuário. Status Pendente = 0 (StatusCodigoVerificacaoOtp).
        // HasFilter é uma API relacional; o provider InMemory usado nos testes não a aplica (ver testes
        // de concorrência para a limitação documentada). A partir de O1.4.3.1, UsuarioId é nullable
        // (candidatos de Bootstrap não têm Usuario ainda) — o filtro exige explicitamente UsuarioId não nulo
        // para nunca ambiguar com a nova linha de candidato de Bootstrap (EmailCandidato).
        builder.HasIndex(x => x.UsuarioId)
            .IsUnique()
            .HasFilter("[Status] = 0 AND [UsuarioId] IS NOT NULL")
            .HasDatabaseName("IX_CodigosVerificacaoOtp_UsuarioId_Pendente");

        // Mesmo princípio para o fluxo de Bootstrap (Work Order O1.4.3, seção 11): no máximo um código
        // Pendente por e-mail candidato — nenhuma corrida de aplicação pode criar dois códigos válidos
        // simultâneos para a mesma tentativa de Bootstrap.
        builder.HasIndex(x => x.EmailCandidato)
            .IsUnique()
            .HasFilter("[Status] = 0 AND [EmailCandidato] IS NOT NULL")
            .HasDatabaseName("IX_CodigosVerificacaoOtp_EmailCandidato_Pendente");
    }
}
