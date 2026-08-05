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
        builder.Property(x => x.Origem).HasMaxLength(30); builder.Property(x => x.Destino).HasMaxLength(30);
        builder.Property(x => x.TimestampComprasOriginal).HasMaxLength(80); builder.Property(x => x.TimestampErpOriginal).HasMaxLength(80);
        builder.Property(x => x.TimestampComprasNormalizado).HasMaxLength(80); builder.Property(x => x.TimestampErpNormalizado).HasMaxLength(80);
        builder.Property(x => x.Decisao).HasMaxLength(40).IsRequired(); builder.Property(x => x.CamposAlterados).HasMaxLength(1000);
        builder.Property(x => x.DadosAntes).HasColumnType("nvarchar(max)"); builder.Property(x => x.DadosDepois).HasColumnType("nvarchar(max)");
        builder.Property(x => x.HashAntes).HasMaxLength(128); builder.Property(x => x.HashDepois).HasMaxLength(128);
        builder.Property(x => x.Tentativa).IsRequired(); builder.Property(x => x.DuracaoMs).IsRequired();
        builder.HasIndex(x => new { x.BusinessUnit, x.ErpSistema, x.ErpFornecedorId, x.ExecutadaEm });
    }
}
