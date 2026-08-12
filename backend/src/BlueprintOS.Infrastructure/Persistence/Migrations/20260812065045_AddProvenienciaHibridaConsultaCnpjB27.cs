using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProvenienciaHibridaConsultaCnpjB27 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PayloadBrutoDescartadoPorTamanho",
                table: "FornecedoresCnpjConsultas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PayloadBrutoJson",
                table: "FornecedoresCnpjConsultas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoErro",
                table: "FornecedoresCnpjConsultas",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FornecedoresCnpjConsultas_DataConsulta",
                table: "FornecedoresCnpjConsultas",
                column: "DataConsulta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FornecedoresCnpjConsultas_DataConsulta",
                table: "FornecedoresCnpjConsultas");

            migrationBuilder.DropColumn(
                name: "PayloadBrutoDescartadoPorTamanho",
                table: "FornecedoresCnpjConsultas");

            migrationBuilder.DropColumn(
                name: "PayloadBrutoJson",
                table: "FornecedoresCnpjConsultas");

            migrationBuilder.DropColumn(
                name: "TipoErro",
                table: "FornecedoresCnpjConsultas");
        }
    }
}
