using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class FornecedorSincronizacaoConfiguration : IEntityTypeConfiguration<FornecedorSincronizacao>
{
    public void Configure(EntityTypeBuilder<FornecedorSincronizacao> builder)
    {
        builder.ToTable("FornecedoresSincronizacoes"); builder.HasKey(x => x.Id);
        builder.Property(x => x.BusinessUnit).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ErpSistema).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ErpFornecedorId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Direcao).HasMaxLength(30).IsRequired(); builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired(); builder.Property(x => x.MensagemErro).HasMaxLength(500);
        builder.HasIndex(x => new { x.BusinessUnit, x.ErpSistema, x.ErpFornecedorId, x.ExecutadaEm });
    }
}
