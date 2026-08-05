using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class FornecedorCnpjConsultaHistoricoConfiguration : IEntityTypeConfiguration<FornecedorCnpjConsultaHistorico>
{
    public void Configure(EntityTypeBuilder<FornecedorCnpjConsultaHistorico> builder)
    {
        builder.ToTable("FornecedoresCnpjConsultas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Cnpj_Cpf).HasColumnType("varchar(14)").HasMaxLength(14).IsRequired();
        builder.Property(x => x.FonteConsulta).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Usuario).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Resultado).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.MensagemErro).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BusinessUnit).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ErpSistema).HasMaxLength(80);
        builder.HasIndex(x => new { x.BusinessUnit, x.Cnpj_Cpf, x.DataConsulta });
        builder.HasIndex(x => x.CorrelationId);
    }
}
