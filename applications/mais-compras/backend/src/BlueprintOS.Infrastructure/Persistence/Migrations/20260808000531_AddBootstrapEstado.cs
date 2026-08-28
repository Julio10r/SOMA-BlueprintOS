using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBootstrapEstado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CodigosVerificacaoOtp_UsuarioId_Pendente",
                table: "CodigosVerificacaoOtp");

            migrationBuilder.AlterColumn<Guid>(
                name: "UsuarioId",
                table: "CodigosVerificacaoOtp",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "EmailCandidato",
                table: "CodigosVerificacaoOtp",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BootstrapEstado",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Concluido = table.Column<bool>(type: "bit", nullable: false),
                    ConcluidoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UsuarioAdministradorSeniorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BootstrapEstado", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BootstrapSessoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailCandidato = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    IdentificadorHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BootstrapSessoes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BootstrapEstado",
                columns: new[] { "Id", "Concluido", "ConcluidoEm", "UsuarioAdministradorSeniorId" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), false, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_CodigosVerificacaoOtp_EmailCandidato_Pendente",
                table: "CodigosVerificacaoOtp",
                column: "EmailCandidato",
                unique: true,
                filter: "[Status] = 0 AND [EmailCandidato] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CodigosVerificacaoOtp_UsuarioId_Pendente",
                table: "CodigosVerificacaoOtp",
                column: "UsuarioId",
                unique: true,
                filter: "[Status] = 0 AND [UsuarioId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BootstrapSessoes_IdentificadorHash",
                table: "BootstrapSessoes",
                column: "IdentificadorHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BootstrapEstado");

            migrationBuilder.DropTable(
                name: "BootstrapSessoes");

            migrationBuilder.DropIndex(
                name: "IX_CodigosVerificacaoOtp_EmailCandidato_Pendente",
                table: "CodigosVerificacaoOtp");

            migrationBuilder.DropIndex(
                name: "IX_CodigosVerificacaoOtp_UsuarioId_Pendente",
                table: "CodigosVerificacaoOtp");

            migrationBuilder.DropColumn(
                name: "EmailCandidato",
                table: "CodigosVerificacaoOtp");

            migrationBuilder.AlterColumn<Guid>(
                name: "UsuarioId",
                table: "CodigosVerificacaoOtp",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodigosVerificacaoOtp_UsuarioId_Pendente",
                table: "CodigosVerificacaoOtp",
                column: "UsuarioId",
                unique: true,
                filter: "[Status] = 0");
        }
    }
}
