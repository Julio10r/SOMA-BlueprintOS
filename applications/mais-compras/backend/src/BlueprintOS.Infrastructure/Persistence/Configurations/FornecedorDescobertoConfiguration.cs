using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class FornecedorDescobertoConfiguration : IEntityTypeConfiguration<FornecedorDescoberto>
{
    public void Configure(EntityTypeBuilder<FornecedorDescoberto> builder)
    {
        builder.ToTable("FornecedoresDescobertos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoItem).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Descricao).HasMaxLength(500);
        builder.Property(x => x.Categoria).HasMaxLength(150);
        builder.Property(x => x.Nome).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Cnpj).HasMaxLength(14);
        builder.Property(x => x.CodigoFornecedor).HasMaxLength(100);
        builder.Property(x => x.Score).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.Criterio).HasMaxLength(30).IsRequired();
        // B3 — Bloco 5A.9 (mesma correção de TemporaryUserId aplicada a Fornecedor): descoberta é
        // corporativa por CodigoItem, não pertence a um usuário. Coluna nullable só por compatibilidade
        // histórica; índice de escopo por usuário removido.
        builder.Property(x => x.TemporaryUserId);
        builder.Property(x => x.DescobertoEm).IsRequired();
        builder.HasIndex(x => x.DescobertoEm);
        builder.HasIndex(x => x.CodigoItem);
    }
}
