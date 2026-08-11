using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class AlcadaAprovacaoConfiguration : IEntityTypeConfiguration<AlcadaAprovacao>
{
    public void Configure(EntityTypeBuilder<AlcadaAprovacao> builder)
    {
        builder.ToTable("AlcadasAprovacao");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Criterio).HasConversion<int>().IsRequired();
        builder.Property(x => x.ValorMinimo).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ValorMaximo).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Nivel).IsRequired();
        builder.Property(x => x.Ativo).IsRequired();
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Sem navegação de FK real para Usuario/Perfil/CentroCustoMetadado deliberadamente: os IDs são
        // referências fracas validadas na camada de aplicação (mesmo padrão de UsuarioCentroCusto, que
        // referencia código ERP sem FK física por ser dado mestre externo) — aqui, no entanto, Usuario e
        // Perfil SÃO tabelas locais, então mantemos índices para consulta/isolamento por Unidade de
        // Negócio sem acoplar constraints de FK que dificultariam a evolução do motor de aprovação.
        builder.HasIndex(x => x.UnidadeNegocioId);
    }
}
