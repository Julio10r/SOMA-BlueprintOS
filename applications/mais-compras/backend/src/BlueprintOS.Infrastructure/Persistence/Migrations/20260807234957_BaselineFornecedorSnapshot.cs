using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <summary>Migration NO-OP intencional (reconciliação O1.4.3.1, MÉDIO/N da Security Validation).
    /// O repositório nunca teve um <c>BlueprintOSDbContextModelSnapshot.cs</c> commitado: as 8 migrations
    /// históricas de Fornecedor (<c>202607300001</c>…<c>202608020001</c>) foram escritas manualmente, sem
    /// nunca passar por <c>dotnet ef migrations add</c>, porque o ambiente Cowork anterior não tinha SDK
    /// .NET. Sem um snapshot de baseline, a primeira execução real de <c>dotnet ef migrations add</c>
    /// (ao gerar <c>AddIdentityAuthentication</c>) tratou o banco como vazio e tentou recriar as 8 tabelas
    /// de Fornecedor que já existem no banco compartilhado.
    ///
    /// Esta migration existe exclusivamente para estabelecer, de forma auditável, o snapshot do EF
    /// correspondente ao schema já aplicado pelas migrations manuscritas — Up() e Down() são
    /// deliberadamente vazios porque nenhuma dessas 8 tabelas deve ser criada ou removida por aqui; elas
    /// já existem no banco. O <c>BlueprintOSDbContextModelSnapshot.cs</c> gerado junto com esta migration
    /// (representando apenas Fornecedores, sem Identity) é o artefato que de fato importa: ele passa a
    /// servir de baseline correto para o diff de todas as migrations subsequentes.</summary>
    public partial class BaselineFornecedorSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intencionalmente vazio — ver doc da classe.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intencionalmente vazio — ver doc da classe.
        }
    }
}
