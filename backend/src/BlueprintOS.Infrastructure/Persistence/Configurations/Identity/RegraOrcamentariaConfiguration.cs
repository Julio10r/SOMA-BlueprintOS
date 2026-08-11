using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class RegraOrcamentariaConfiguration : IEntityTypeConfiguration<RegraOrcamentaria>
{
    public void Configure(EntityTypeBuilder<RegraOrcamentaria> builder)
    {
        builder.ToTable("RegrasOrcamentarias");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ValorLimite).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Periodo).HasConversion<int>().IsRequired();
        builder.Property(x => x.Ativo).IsRequired();
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Índice em (UnidadeNegocioId, CentroCustoMetadadoId, Periodo) — mesmo desenho documentado em
        // ComprasDataModel.md para consulta e futura validação de regra duplicada (não uma constraint de
        // unicidade nesta fundação: nada impede, ainda, duas regras para o mesmo Centro de Custo/Período
        // com nomes distintos — decisão de produto pendente).
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.CentroCustoMetadadoId, x.Periodo });
    }
}
