using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CodigosVerificacaoOtp",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "OtpRequestThrottles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailNormalizado = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    JanelaIniciadaEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SolicitacoesNaJanela = table.Column<int>(type: "int", nullable: false),
                    UltimaSolicitacaoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpRequestThrottles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodigosVerificacaoOtp_UsuarioId_Pendente",
                table: "CodigosVerificacaoOtp",
                column: "UsuarioId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OtpRequestThrottles_EmailNormalizado",
                table: "OtpRequestThrottles",
                column: "EmailNormalizado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpRequestThrottles");

            migrationBuilder.DropIndex(
                name: "IX_CodigosVerificacaoOtp_UsuarioId_Pendente",
                table: "CodigosVerificacaoOtp");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CodigosVerificacaoOtp");
        }
    }
}
