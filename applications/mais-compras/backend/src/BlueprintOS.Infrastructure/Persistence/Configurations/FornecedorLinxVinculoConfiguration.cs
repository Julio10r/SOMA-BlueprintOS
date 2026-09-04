using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

/// <summary>B3 — Bloco 5A.9: vínculos Linx de um Fornecedor (modelo 1 CNPJ = 1 Fornecedor, N vínculos —
/// decisão do Product Owner, GAPs KALUNGA/PLATINUM). Aggregate root próprio (mesmo padrão de
/// <see cref="SincronizacaoFornecedor"/> — sem navegação de coleção em <c>Fornecedor</c>).</summary>
public sealed class FornecedorLinxVinculoConfiguration : IEntityTypeConfiguration<FornecedorLinxVinculo>
{
    public void Configure(EntityTypeBuilder<FornecedorLinxVinculo> builder)
    {
        builder.ToTable("FornecedorLinxVinculos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FornecedorId).IsRequired();
        builder.Property(x => x.UnidadeNegocioId).IsRequired();
        builder.Property(x => x.ErpSistema).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CodigoErp).HasMaxLength(80).IsRequired();
        builder.Property(x => x.NomeClifor).HasMaxLength(200).IsRequired();
        builder.Property(x => x.InativoFornecedores).IsRequired();
        builder.Property(x => x.InativoCadastroCliFor).IsRequired();
        builder.Property(x => x.DataParaTransferencia);
        builder.Property(x => x.Principal).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();
        builder.Ignore(x => x.Ativo);

        // Identidade ERP do vínculo — nunca o CNPJ (esse identifica o Fornecedor, não o vínculo). Onda 2
        // (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): UnidadeNegocioId compõe a identidade
        // — o mesmo CodigoErp pode existir em instâncias Linx de BUs diferentes sem colidir.
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.ErpSistema, x.CodigoErp })
            .IsUnique()
            .HasDatabaseName("IX_FornecedorLinxVinculos_UnidadeNegocioId_ErpSistema_CodigoErp");

        // Nomeados explicitamente via o overload HasIndex(propriedade, nome): duas chamadas HasIndex(x =>
        // x.FornecedorId) sem nome explícito na assinatura são tratadas pelo EF Core como o MESMO índice
        // (identidade por lista de propriedades), e a segunda configuração sobrescreveria a primeira.
        builder.HasIndex(x => x.FornecedorId, "IX_FornecedorLinxVinculos_FornecedorId");

        // Invariante "no máximo um vínculo ATIVO Principal por Fornecedor" (decisão do Product Owner) —
        // rede de segurança no nível do banco, não a única defesa (o caso de uso valida antes de escrever).
        // Um vínculo Principal que ficou inativo (Principal=true, Ativo=false) NUNCA colide com este índice
        // — é exatamente o que preserva o "Principal histórico" sem impedir um novo Principal ativo.
        builder.HasIndex(x => x.FornecedorId, "IX_FornecedorLinxVinculos_FornecedorId_PrincipalAtivo")
            .IsUnique()
            .HasFilter("[Principal] = 1 AND [InativoFornecedores] = 0 AND [InativoCadastroCliFor] = 0");
    }
}
