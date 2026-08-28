using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlueprintOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAIGovernanceTablesFromMaisCompras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIGovernanceApprovalGrants");

            migrationBuilder.DropTable(
                name: "AIGovernanceAuditEvents");

            migrationBuilder.DropTable(
                name: "AIGovernanceRecoveryIndex");

            migrationBuilder.DropTable(
                name: "AIGovernanceRollbackAudit");

            migrationBuilder.DropTable(
                name: "AIGovernanceWriteExecutionAudit");

            migrationBuilder.DropTable(
                name: "AIGovernanceWriteValidationKnowledgeGaps");

            migrationBuilder.DropTable(
                name: "AIGovernanceWriteVerificationProfiles");

            migrationBuilder.DropTable(
                name: "AIGovernanceApprovalRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIGovernanceApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProposalHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequiredApprover = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RiskClassification = table.Column<int>(type: "int", nullable: false),
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
                    ActionProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    CategoriesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ProposalHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGovernanceAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIGovernanceRecoveryIndex",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    BackupRequired = table.Column<bool>(type: "bit", nullable: false),
                    BusinessKeysJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ConnectionProfile = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Database = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExecutionName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ManifestChecksumSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationTypesJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PackagePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProposalHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecordsAffected = table.Column<int>(type: "int", nullable: false),
                    Requester = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    RollbackSupported = table.Column<bool>(type: "bit", nullable: false),
                    Server = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TablesAffectedJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ValidationRuleId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGovernanceRecoveryIndex", x => x.ExecutionId);
                });

            migrationBuilder.CreateTable(
                name: "AIGovernanceRollbackAudit",
                columns: table => new
                {
                    RollbackExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessKeysJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ConcurrencyFindingsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExpectedStateSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExplicitConfirmationReceived = table.Column<bool>(type: "bit", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ObservedStateSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OriginalExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostRollbackValidationPassed = table.Column<bool>(type: "bit", nullable: false),
                    PostRollbackValidationRuleId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    RecordsAffected = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Requester = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RollbackProposalHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TablesAffectedJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGovernanceRollbackAudit", x => x.RollbackExecutionId);
                });

            migrationBuilder.CreateTable(
                name: "AIGovernanceWriteExecutionAudit",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    BackupCreated = table.Column<bool>(type: "bit", nullable: false),
                    BackupExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BackupRequired = table.Column<bool>(type: "bit", nullable: false),
                    BeforeAfterSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BusinessKeysJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ChangedFieldsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConnectionProfile = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Database = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ErrorsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExecutionName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    KnowledgeGapsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OperationsJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    PostWriteValidationPassed = table.Column<bool>(type: "bit", nullable: false),
                    ProceduresInvokedJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ProposalHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecordsAffected = table.Column<int>(type: "int", nullable: false),
                    RecordsValidated = table.Column<int>(type: "int", nullable: false),
                    RecordsWithErrors = table.Column<int>(type: "int", nullable: false),
                    RecoveryPackageStatus = table.Column<int>(type: "int", nullable: false),
                    Requester = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    RollbackAvailable = table.Column<bool>(type: "bit", nullable: false),
                    RollbackExecuted = table.Column<bool>(type: "bit", nullable: false),
                    RollbackResult = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Server = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TablesAffectedJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ValidationRuleId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    WriteVerificationPolicyVersion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGovernanceWriteExecutionAudit", x => x.ExecutionId);
                });

            migrationBuilder.CreateTable(
                name: "AIGovernanceWriteValidationKnowledgeGaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ConnectionProfile = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGovernanceWriteValidationKnowledgeGaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIGovernanceWriteVerificationProfiles",
                columns: table => new
                {
                    ConnectionProfile = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    BackupRequired = table.Column<bool>(type: "bit", nullable: false),
                    BackupRetentionDays = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PostWriteValidationRequired = table.Column<bool>(type: "bit", nullable: false),
                    RollbackSupported = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGovernanceWriteVerificationProfiles", x => new { x.ConnectionProfile, x.PolicyVersion });
                });

            migrationBuilder.CreateTable(
                name: "AIGovernanceApprovalGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProposalHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
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

            migrationBuilder.InsertData(
                table: "AIGovernanceWriteVerificationProfiles",
                columns: new[] { "ConnectionProfile", "PolicyVersion", "ApprovedBy", "BackupRequired", "BackupRetentionDays", "EffectiveFrom", "PostWriteValidationRequired", "RollbackSupported" },
                values: new object[,]
                {
                    { "linx-development", "1.0-phase-a", "product-owner", true, 30, new DateTimeOffset(new DateTime(2026, 8, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, true },
                    { "linx-development", "2.0-phase-b", "product-owner", false, 0, new DateTimeOffset(new DateTime(2099, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false },
                    { "linx-production", "1.0", "product-owner", true, 30, new DateTimeOffset(new DateTime(2026, 8, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, true },
                    { "wise", "1.0-config-only", "product-owner", false, 0, new DateTimeOffset(new DateTime(2026, 8, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, false }
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

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceRecoveryIndex_AgentId_ExecutedAt",
                table: "AIGovernanceRecoveryIndex",
                columns: new[] { "AgentId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceRecoveryIndex_ConnectionProfile_ExecutedAt",
                table: "AIGovernanceRecoveryIndex",
                columns: new[] { "ConnectionProfile", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceRecoveryIndex_Requester",
                table: "AIGovernanceRecoveryIndex",
                column: "Requester");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceRecoveryIndex_Status_ExpiresAt",
                table: "AIGovernanceRecoveryIndex",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceRollbackAudit_OriginalExecutionId",
                table: "AIGovernanceRollbackAudit",
                column: "OriginalExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceRollbackAudit_RequestedAt",
                table: "AIGovernanceRollbackAudit",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceWriteExecutionAudit_AgentId_StartedAt",
                table: "AIGovernanceWriteExecutionAudit",
                columns: new[] { "AgentId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceWriteExecutionAudit_ConnectionProfile_StartedAt",
                table: "AIGovernanceWriteExecutionAudit",
                columns: new[] { "ConnectionProfile", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceWriteExecutionAudit_Outcome",
                table: "AIGovernanceWriteExecutionAudit",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceWriteExecutionAudit_Requester",
                table: "AIGovernanceWriteExecutionAudit",
                column: "Requester");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceWriteValidationKnowledgeGaps_DetectedAt",
                table: "AIGovernanceWriteValidationKnowledgeGaps",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceWriteValidationKnowledgeGaps_Resource_Operation",
                table: "AIGovernanceWriteValidationKnowledgeGaps",
                columns: new[] { "Resource", "Operation" });

            migrationBuilder.CreateIndex(
                name: "IX_AIGovernanceWriteVerificationProfiles_ConnectionProfile_EffectiveFrom",
                table: "AIGovernanceWriteVerificationProfiles",
                columns: new[] { "ConnectionProfile", "EffectiveFrom" });
        }
    }
}
