using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class CategoriaFornecedorConfiguration : IEntityTypeConfiguration<CategoriaFornecedor>
{
    public void Configure(EntityTypeBuilder<CategoriaFornecedor> builder)
    {
        builder.ToTable("CategoriasFornecedor");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Descricao).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Ativo).IsRequired();
        builder.HasIndex(x => x.Codigo).IsUnique();

        // Seed inicial (Gate de homologação, 2026-09-01) — catálogo pré-cadastrado pedido pelo
        // homologador para substituir o campo Categoria em texto livre. Ids fixos para o seed ser
        // determinístico entre ambientes (mesma convenção de HasData usada em outras migrations).
        builder.HasData(
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-000000000001"), Codigo = "MATERIA_PRIMA", Descricao = "Matéria-Prima", Ativo = true },
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-000000000002"), Codigo = "EMBALAGEM", Descricao = "Embalagem", Ativo = true },
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-000000000003"), Codigo = "SERVICOS_GERAIS", Descricao = "Serviços Gerais", Ativo = true },
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-000000000004"), Codigo = "TRANSPORTE_LOGISTICA", Descricao = "Transporte e Logística", Ativo = true },
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-000000000005"), Codigo = "MARKETING_PUBLICIDADE", Descricao = "Marketing e Publicidade", Ativo = true },
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-000000000006"), Codigo = "TECNOLOGIA_INFORMACAO", Descricao = "Tecnologia da Informação", Ativo = true },
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-000000000007"), Codigo = "MANUTENCAO_FACILITIES", Descricao = "Manutenção e Facilities", Ativo = true },
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-000000000008"), Codigo = "CONSULTORIA", Descricao = "Consultoria", Ativo = true },
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-000000000009"), Codigo = "EQUIPAMENTOS", Descricao = "Equipamentos", Ativo = true },
            new { Id = new Guid("8a9f1b1a-0001-4a00-9a00-00000000000a"), Codigo = "OUTROS", Descricao = "Outros", Ativo = true }
        );
    }
}
