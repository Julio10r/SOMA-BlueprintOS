using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class ErroSincronizacaoFornecedorConfiguration : IEntityTypeConfiguration<ErroSincronizacaoFornecedor>
{
    public void Configure(EntityTypeBuilder<ErroSincronizacaoFornecedor> builder)
    {
        builder.ToTable("ErrosSincronizacoesFornecedores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FornecedorIdentificacao).HasMaxLength(160);
        builder.Property(x => x.Mensagem).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.StackTrace).HasMaxLength(2000);
        builder.Property(x => x.DataHora).IsRequired();
        builder.HasIndex(x => x.SincronizacaoFornecedorId);
        builder.HasIndex(x => x.DataHora);
    }
}
