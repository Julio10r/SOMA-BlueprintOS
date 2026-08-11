using System.Text.Json;
using BlueprintOS.Domain.Knowledge.Linx;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Knowledge;

public sealed class LinxKnowledgeEntryConfiguration : IEntityTypeConfiguration<LinxKnowledgeEntry>
{
    public void Configure(EntityTypeBuilder<LinxKnowledgeEntry> builder)
    {
        builder.ToTable("LinxConhecimentoEntradas");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Especialista).HasConversion<int>().IsRequired();
        builder.Property(x => x.Categoria).HasConversion<int>().IsRequired();
        builder.Property(x => x.Assunto).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Conteudo).IsRequired().HasMaxLength(8000);
        builder.Property(x => x.Proveniencia).HasConversion<int>().IsRequired();
        builder.Property(x => x.Fonte).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Ator).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Versao).IsRequired();
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

        // Tags como JSON simples — coleção pequena, sem necessidade de tabela própria nesta fundação
        // (Work Order, seção 13 — MVP pragmático de recuperação).
        builder.Property(x => x.Tags)
            .HasConversion(
                tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<string[]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
                    tags => tags.Aggregate(0, (hash, tag) => HashCode.Combine(hash, tag.GetHashCode())),
                    tags => tags.ToArray()))
            .HasMaxLength(2000);

        // Nunca a mesma linha muda de Id — nenhuma FK declarada para EntradaAnteriorId/VersaoRaizId
        // (auto-referência sobre a mesma tabela, resolvida em código, não por navegação EF) para evitar
        // ciclos de exclusão em cascata sobre dado de auditoria/proveniência.
        builder.HasIndex(x => x.VersaoRaizId);
        builder.HasIndex(x => new { x.Especialista, x.Categoria });
        builder.HasIndex(x => x.UnidadeNegocioId);
    }
}
