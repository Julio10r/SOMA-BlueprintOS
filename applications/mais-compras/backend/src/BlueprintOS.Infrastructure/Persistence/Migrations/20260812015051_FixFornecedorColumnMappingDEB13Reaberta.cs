using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixFornecedorColumnMappingDEB13Reaberta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration puramente de reconciliação de modelo — SEM efeito de schema. A migration
            // `B212FornecedorLinxCanonicalModel` (02/08/2026) já renomeou fisicamente estas colunas para
            // `RazaoSocial`/`Cnpj_Cpf` em todo ambiente onde já foi aplicada (confirmado no banco de
            // desenvolvimento real durante a validação funcional do #41, Gate Final da Onda 1). O que
            // estava desatualizado era só `FornecedorConfiguration.cs`, que continuava com
            // `HasColumnName("Nome")`/`HasColumnName("Cnpj")` — por isso o EF gerava SQL citando colunas
            // físicas inexistentes (`Invalid column name 'Nome'/'Cnpj'`). Corrigida a configuração
            // (removidos os `HasColumnName`, a convenção já bate com a coluna física real); esta
            // migration só existe para que o snapshot do EF pare de "esperar" as colunas antigas —
            // aplicar `RenameColumn`/`RenameIndex` de verdade aqui falharia com o mesmo erro, porque as
            // colunas/índices de origem (`Nome`/`Cnpj`) não existem mais.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Simétrico ao Up() — sem efeito de schema, pelo mesmo motivo.
        }
    }
}
