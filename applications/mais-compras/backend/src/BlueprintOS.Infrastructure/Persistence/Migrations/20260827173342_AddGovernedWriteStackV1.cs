using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernedWriteStackV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIGovernanceApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposalHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RiskClassification = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequiredApprover = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGovernanceApprovalRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIGovernanceAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ActionProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProposalHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AgentId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    SubjectId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CategoriesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGovernanceAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIGovernanceApprovalGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposalHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGovernanceApprovalGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIGovernanceApprovalGrants_AIGovernanceApprovalRequests_ApprovalRequestId",
                        column: x => x.ApprovalRequestId,
                        principalTable: "AIGovernanceApprovalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceApprovalGrants_ApprovalRequestId",
                table: "AIGovernanceApprovalGrants",
                column: "ApprovalRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceApprovalGrants_ProposalHash",
                table: "AIGovernanceApprovalGrants",
                column: "ProposalHash");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceApprovalRequests_ProposalHash",
                table: "AIGovernanceApprovalRequests",
                column: "ProposalHash");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceApprovalRequests_Status_ExpiresAt",
                table: "AIGovernanceApprovalRequests",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceAuditEvents_ActionProposalId",
                table: "AIGovernanceAuditEvents",
                column: "ActionProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceAuditEvents_RequestId_CreatedAt",
                table: "AIGovernanceAuditEvents",
                columns: new[] { "RequestId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIGovernanceApprovalGrants");

            migrationBuilder.DropTable(
                name: "AIGovernanceAuditEvents");

            migrationBuilder.DropTable(
                name: "AIGovernanceApprovalRequests");
        }
    }
}
