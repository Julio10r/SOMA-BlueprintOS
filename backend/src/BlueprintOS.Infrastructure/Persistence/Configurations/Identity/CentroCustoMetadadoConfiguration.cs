using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

/// <summary>O1.7 — metadados locais +Compras de Centro de Custo. Sem FK para o Centro de Custo em si: não
/// existe tabela local do dado mestre (ele permanece só no ERP, ADR-0020 item 3) —
/// <see cref="CentroCustoMetadado.CodigoErp"/> é apenas uma chave de correlação em texto.
///
/// Resolução da dívida O1.6-L2 (ver <c>UsuarioUseCases</c>): esta tabela também serve de ANCORA de Unidade
/// de Negócio para o vínculo Usuário×Centro de Custo — em vez de uma FK física para `UsuariosCentrosCusto`
/// (que exigiria criar o metadado antes de qualquer vínculo, mesmo sem nenhuma necessidade de edição local),
/// a validação ocorre em tempo de execução no caso de uso, consultando este repositório e o
/// <c>ICentroCustoErpReader</c>. Ver relatório final da O1.7 para a justificativa completa.</summary>
public sealed class CentroCustoMetadadoConfiguration : IEntityTypeConfiguration<CentroCustoMetadado>
{
    public void Configure(EntityTypeBuilder<CentroCustoMetadado> builder)
    {
        builder.ToTable("CentrosCustoMetadados");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoErp).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DescricaoMaisCompras).HasMaxLength(400);
        builder.Property(x => x.AtivoNoMaisCompras).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Único GLOBALMENTE por CodigoErp (não só por Unidade de Negócio): é exatamente essa restrição que
        // impede um mesmo código ERP de Centro de Custo ser ancorado a duas Unidades de Negócio diferentes,
        // fechando o vetor de vínculo cross-BU descrito na dívida O1.6-L2.
        builder.HasIndex(x => x.CodigoErp)
            .IsUnique()
            .HasDatabaseName("IX_CentrosCustoMetadados_CodigoErp");
    }
}
