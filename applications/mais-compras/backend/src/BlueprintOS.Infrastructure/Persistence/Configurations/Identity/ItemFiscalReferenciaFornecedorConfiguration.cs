using BlueprintOS.Domain.Identity;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

/// <summary>B3 — Bloco 4: referências de Item Fiscal por Fornecedor. Diferente dos cadastros de apoio dos
/// Blocos 1/2 (sem FK, correlação por código ERP em texto), aqui Item Fiscal e Fornecedor são ambos
/// entidades locais reais do +Compras — FK física para as duas, `Restrict` em ambas (nenhum dos dois pais é
/// removido fisicamente hoje: Item Fiscal só inativa, Fornecedor só inativa — DR-18).</summary>
public sealed class ItemFiscalReferenciaFornecedorConfiguration : IEntityTypeConfiguration<ItemFiscalReferenciaFornecedor>
{
    public void Configure(EntityTypeBuilder<ItemFiscalReferenciaFornecedor> builder)
    {
        builder.ToTable("ItensFiscaisReferenciasFornecedor");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoItemFornecedor).IsRequired().HasMaxLength(60);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Estrutura comprovada em Linx (docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md):
        // ITEM_FISCAL_REF_FORNECEDOR.KeyFieldList = FORNECEDOR, CODIGO_ITEM — um Fornecedor tem no máximo
        // uma referência por Item Fiscal.
        builder.HasIndex(x => new { x.ItemFiscalId, x.FornecedorId })
            .IsUnique()
            .HasDatabaseName("IX_ItensFiscaisReferenciasFornecedor_ItemFiscalId_FornecedorId");

        // DECISÃO DO PRODUCT OWNER (homologação do Bloco 4, 2026-09-02): unicidade GLOBAL de
        // (FornecedorId, CodigoItemFornecedor) — não comprovada em Linx (a KeyFieldList real não cobre esta
        // direção), explicitamente autorizada para garantir que o DE/PARA reverso (Fornecedor + código
        // usado pelo fornecedor → Item Fiscal) sempre resolva para um único Item Fiscal, pré-requisito do
        // processamento futuro de XML NF-e/NFS-e (Bloco 5B+).
        builder.HasIndex(x => new { x.FornecedorId, x.CodigoItemFornecedor })
            .IsUnique()
            .HasDatabaseName("IX_ItensFiscaisReferenciasFornecedor_FornecedorId_CodigoItemFornecedor");

        builder.HasOne<ItemFiscal>()
            .WithMany()
            .HasForeignKey(x => x.ItemFiscalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Fornecedor>()
            .WithMany()
            .HasForeignKey(x => x.FornecedorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
