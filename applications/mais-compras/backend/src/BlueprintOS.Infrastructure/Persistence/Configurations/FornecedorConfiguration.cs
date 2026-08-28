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

        // Achado da validação funcional do entregável #41 (Gate Final da Onda 1, continuação
        // 12/08/2026) — DEB-13 reaberta: a migration `B212FornecedorLinxCanonicalModel` (02/08/2026) já
        // renomeou fisicamente as colunas legadas `Nome`→`RazaoSocial` e `Cnpj`→`Cnpj_Cpf`, mas esta
        // configuração nunca foi atualizada para acompanhar — continuava instruindo o EF a gerar SQL
        // contra os nomes físicos ANTIGOS. Sem reproduzir em `dotnet test` (SQLite/InMemory nos testes
        // não validam nomes de coluna reais) nem em `dotnet ef migrations has-pending-model-changes`
        // (compara o modelo atual contra o snapshot, ambos gerados a partir deste mesmo código — logo
        // sempre concordam entre si, mesmo quando ambos divergem do banco real). Só reproduziu ao
        // conectar no banco de desenvolvimento real (SELECT gerado citava `[Nome]`/`[Cnpj]`, que não
        // existem mais na tabela física) — a mesma conclusão anterior de "não reproduz" (auditoria
        // O1.14) estava desatualizada porque, à época, o ambiente sem VPN impedia a verificação contra o
        // schema físico real. Removidos os `HasColumnName` — a convenção (nome da propriedade = nome da
        // coluna) já é exatamente o que a tabela física tem hoje.
        builder.Property(x => x.RazaoSocial)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Cnpj_Cpf)
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
        // CNAE principal (B2.8) — complementar/opcional, sem coluna equivalente prévia na tabela
        // física. Código em dígitos puros (máscara é apresentação); descrição livre da fonte externa.
        builder.Property(x => x.CnaePrincipalCodigo).HasMaxLength(7);
        builder.Property(x => x.CnaePrincipalDescricao).HasMaxLength(300);
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