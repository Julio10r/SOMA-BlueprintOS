using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

/// <summary>B3 — Bloco 3: cadastro local de Item Fiscal. Sem FK física para os cadastros de apoio (Conta
/// Contábil/Unidade de Medida): ambos são correlacionados por código em texto, validados no caso de uso
/// contra a leitura combinada ERP+metadados locais (mesmo motivo de <c>FilialMetadado</c>/
/// <c>CentroCustoMetadado</c> não terem FK — um código de apoio pode ser válido no ERP sem nunca ter tido
/// metadado local criado).</summary>
public sealed class ItemFiscalConfiguration : IEntityTypeConfiguration<ItemFiscal>
{
    public void Configure(EntityTypeBuilder<ItemFiscal> builder)
    {
        builder.ToTable("ItensFiscais");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Descricao).IsRequired().HasMaxLength(400);
        builder.Property(x => x.UnidadeMedidaCodigoErp).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ContaContabilCodigoErp).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Ativo).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Único GLOBALMENTE (não por Unidade de Negócio): mesma decisão já adotada para DocumentoFiscal de
        // Fornecedor (ADR-0023) — o Item Fiscal deverá corresponder 1:1 a um único CADASTRO_ITEM_FISCAL.
        // CODIGO_ITEM do Linx quando a sincronização (Bloco 5) existir; escopar por BU agora criaria
        // colisão/retrabalho de reconciliação nesse momento futuro.
        builder.HasIndex(x => x.Codigo)
            .IsUnique()
            .HasDatabaseName("IX_ItensFiscais_Codigo");

        builder.HasIndex(x => x.UnidadeNegocioId)
            .HasDatabaseName("IX_ItensFiscais_UnidadeNegocioId");
    }
}
