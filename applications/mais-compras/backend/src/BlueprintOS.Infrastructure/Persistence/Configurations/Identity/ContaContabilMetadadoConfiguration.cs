using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

/// <summary>B3 — Bloco 1: metadados locais +Compras de Conta Contábil. Sem FK para a Conta Contábil em
/// si: não existe tabela local do dado mestre (ele permanece só no ERP) —
/// <see cref="ContaContabilMetadado.CodigoErp"/> é apenas uma chave de correlação em texto.</summary>
public sealed class ContaContabilMetadadoConfiguration : IEntityTypeConfiguration<ContaContabilMetadado>
{
    public void Configure(EntityTypeBuilder<ContaContabilMetadado> builder)
    {
        builder.ToTable("ContasContabeisMetadados");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DescricaoMaisCompras).HasMaxLength(400);
        builder.Property(x => x.AtivoNoMaisCompras).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Único GLOBALMENTE por CodigoErp (não por Unidade de Negócio): Conta Contábil é um cadastro de
        // apoio do plano de contas do Linx, compartilhado entre Unidades de Negócio (mesma decisão de
        // CentroCustoMetadadoConfiguration) — um mesmo código não deve ter dois metadados locais divergentes.
        builder.HasIndex(x => x.CodigoErp)
            .IsUnique()
            .HasDatabaseName("IX_ContasContabeisMetadados_CodigoErp");
    }
}
