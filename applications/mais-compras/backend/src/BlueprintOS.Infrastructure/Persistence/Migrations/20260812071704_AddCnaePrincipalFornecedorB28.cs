using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCnaePrincipalFornecedorB28 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CnaePrincipalCodigo",
                table: "Fornecedores",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CnaePrincipalDescricao",
                table: "Fornecedores",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CnaePrincipalCodigo",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "CnaePrincipalDescricao",
                table: "Fornecedores");
        }
    }
}
