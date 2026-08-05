using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class FornecedorConfiguration : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> builder)
    {
        builder.ToTable("Fornecedores");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RazaoSocial)
            .HasColumnName("Nome")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Cnpj_Cpf)
            .HasColumnName("Cnpj")
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(x => x.Categoria).HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.Telefone).HasMaxLength(30);
        builder.Property(x => x.Website).HasMaxLength(500);
        builder.Property(x => x.Cidade).HasMaxLength(100);
        builder.Property(x => x.Estado).HasMaxLength(100);
        builder.Property(x => x.Pais).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ScoreIA).HasPrecision(5, 2);
        builder.Property(x => x.TemporaryUserId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.Ignore(x => x.Nome);
        builder.Ignore(x => x.Cnpj);

        builder.HasIndex(x => x.Cnpj_Cpf).IsUnique();
        builder.HasIndex(x => x.RazaoSocial);
        builder.HasIndex(x => x.TemporaryUserId);
    }
}