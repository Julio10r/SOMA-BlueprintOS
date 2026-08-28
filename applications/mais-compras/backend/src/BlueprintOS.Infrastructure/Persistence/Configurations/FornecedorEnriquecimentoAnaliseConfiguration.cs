using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class FornecedorEnriquecimentoAnaliseConfiguration : IEntityTypeConfiguration<FornecedorEnriquecimentoAnalise>
{
    public void Configure(EntityTypeBuilder<FornecedorEnriquecimentoAnalise> builder)
    {
        builder.ToTable("FornecedoresEnriquecimentoAnalises"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Cnpj_Cpf).HasColumnType("varchar(14)").HasMaxLength(14).IsRequired();
        builder.Property(x => x.Campo).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ValorAnterior).HasMaxLength(500);
        builder.Property(x => x.ValorNovo).HasMaxLength(500);
        builder.Property(x => x.Decisao).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.BusinessUnit).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ErpSistema).HasMaxLength(80);
        builder.Property(x => x.Fonte).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => new { x.FornecedorId, x.Campo, x.DataHora });
        builder.HasIndex(x => x.CorrelationId);
    }
}
