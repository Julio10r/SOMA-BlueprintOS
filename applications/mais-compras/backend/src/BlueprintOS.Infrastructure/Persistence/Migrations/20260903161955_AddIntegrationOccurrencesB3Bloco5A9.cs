using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationOccurrencesB3Bloco5A9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationOccurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dataset = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OriginRecordKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContextoTecnico = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationOccurrences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOccurrences_Dataset_Status",
                table: "IntegrationOccurrences",
                columns: new[] { "Dataset", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOccurrences_Dedup",
                table: "IntegrationOccurrences",
                columns: new[] { "ExecutionId", "Dataset", "Stage", "Code", "OriginRecordKey" },
                unique: true,
                filter: "[OriginRecordKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOccurrences_ExecutionId",
                table: "IntegrationOccurrences",
                column: "ExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationOccurrences");
        }
    }
}
