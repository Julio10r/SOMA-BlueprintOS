using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixAdministradorSeniorPermissoesCompletasO1Gate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Achado da validação funcional do entregável #41 (Gate Final da Onda 1, continuação
            // 12/08/2026): três migrations posteriores a `AddRbacPerfilPermissaoCatalogo` (O1.5) —
            // `AddAdministracaoWorkflowAlcadaOrcamentoO112` e `AddLinxKnowledgeO1135` — adicionaram 5
            // permissões novas ao catálogo (`Workflow.Gerenciar`, `Alcada.Gerenciar`,
            // `Orcamento.Gerenciar`, `ConhecimentoLinx.Gerenciar`, `ConhecimentoLinx.Aprovar`) sem
            // reexecutar o backfill para os Perfis "Administrador Sênior" já existentes — só o Bootstrap
            // (que roda uma única vez por instalação) concede o catálogo completo automaticamente.
            // Resultado real observado no banco de desenvolvimento: o Administrador Sênior criado pelo
            // Bootstrap antes da O1.12/O1.13.5 tinha apenas 14 das 19 permissões do catálogo — 403 real
            // ao acessar Alçadas, Regras de Workflow, Regras Orçamentárias e os endpoints de Conhecimento
            // Linx, apesar de essas telas estarem implementadas e o ator ser o administrador mais
            // privilegiado do sistema.
            //
            // Backfill idempotente (`NOT EXISTS`, mesmo padrão de `AddRbacPerfilPermissaoCatalogo`), para
            // TODO Perfil "Administrador Sênior" existente em qualquer Unidade de Negócio — nunca cria
            // Perfil novo, nunca toca em nenhum outro Perfil.
            migrationBuilder.Sql(@"
INSERT INTO [PerfisPermissoes] ([PerfilId], [PermissaoId])
SELECT p.[Id], perm.[Id]
  FROM [Perfis] p
 CROSS JOIN [Permissoes] perm
 WHERE p.[Nome] = N'Administrador Sênior'
   AND NOT EXISTS (
        SELECT 1 FROM [PerfisPermissoes] pp
         WHERE pp.[PerfilId] = p.[Id] AND pp.[PermissaoId] = perm.[Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversível por design: não há como distinguir, no Down, quais vínculos preexistiam à
            // aplicação deste backfill dos que foram concedidos por ele — reverter poderia remover
            // vínculos legítimos anteriores. Mesma decisão já tomada implicitamente por
            // `AddRbacPerfilPermissaoCatalogo`, cujo Down também não desfaz o backfill de permissões.
        }
    }
}
