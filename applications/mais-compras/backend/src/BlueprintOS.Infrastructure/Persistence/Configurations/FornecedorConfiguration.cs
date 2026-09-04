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

        // Onda 2 (Multi-BU/Multi-ERP) — fronteira de dados do Fornecedor, ver doc-comment da entidade.
        builder.Property(x => x.UnidadeNegocioId).IsRequired();

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
        // B3 — Bloco 5A.9 (correção do resíduo arquitetural TemporaryUserId, decisão do Product Owner):
        // Fornecedor é corporativo, não pertence a um usuário. Coluna deixa de ser obrigatória e de ter
        // índice de escopo — nenhuma consulta filtra mais por ela. Mantida nullable só por compatibilidade
        // histórica dos 27.757 registros existentes (ver doc comment em Fornecedor.TemporaryUserId).
        builder.Property(x => x.TemporaryUserId);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.Ignore(x => x.Nome);
        builder.Ignore(x => x.Cnpj);

        // Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner registrada em
        // applications/mais-compras/docs/cadernos/Onda-2.md): identidade funcional passa a ser
        // (UnidadeNegocioId, Cnpj_Cpf), não mais Cnpj_Cpf globalmente único — 1 CNPJ/CPF = 1 Fornecedor
        // DENTRO da Unidade de Negócio; o mesmo CNPJ pode existir como Fornecedores independentes em BUs
        // diferentes (Grupo Soma/CNPJ X e Reserva/CNPJ X nunca são obrigatoriamente o mesmo registro).
        // Normalização arquitetural para multi-BU, não reabertura do Gate Fornecedores já homologado
        // (01/09/2026) para a Grupo Soma.
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.Cnpj_Cpf })
            .IsUnique()
            .HasDatabaseName("IX_Fornecedores_UnidadeNegocioId_Cnpj_Cpf");
        builder.HasIndex(x => x.RazaoSocial);

        // B3 — Bloco 5A (diagnóstico ErpFornecedorId, docs/audits/B3-Bloco5A-*.md): restaura a proteção de
        // unicidade da identidade ERP canônica escolhida pelo Product Owner para relacionamentos
        // (CLIFOR/ErpFornecedorId). Um índice composto único equivalente existiu (migration
        // B21FornecedorSynchronization) e foi removido por regressão acidental (commit 7bf3bf4, remoção de
        // Docker) — nunca por decisão de design; nenhum documento indica fornecedores legitimamente
        // compartilhando ErpFornecedorId entre BUs. Dado real de Produção (02/09/2026) confirma 0
        // duplicidade em 27.754 registros com ErpFornecedorId preenchido. Onda 2: UnidadeNegocioId
        // acrescentada à composição (antes só ErpSistema) — o mesmo CLIFOR pode, em tese, existir em
        // instâncias Linx de BUs diferentes; filtrado para não conflitar com os registros locais sem
        // identidade ERP ainda (OrigemInformacao = MaisCompras).
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.ErpSistema, x.ErpFornecedorId })
            .IsUnique()
            .HasFilter("[ErpFornecedorId] IS NOT NULL")
            .HasDatabaseName("IX_Fornecedores_UnidadeNegocioId_ErpSistema_ErpFornecedorId");
    }
}