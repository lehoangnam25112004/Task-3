using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConstructionProjectManagement.Migrations
{
    public partial class Task03DocumentManagementCompletion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentAuditLogs_ProjectDocuments_DocumentId",
                table: "DocumentAuditLogs");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<int>(
                name: "CurrentApprovalStep",
                table: "ProjectDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
            migrationBuilder.AddColumn<DateTime>(
                name: "RestoredAt",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "RestoredByUserId",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "SubmittedByUserId",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DocumentVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "DocumentVersions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "FileExtension",
                table: "DocumentVersions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "DocumentVersions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
            migrationBuilder.AddColumn<string>(
                name: "Sha256Hash",
                table: "DocumentVersions",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "VersionNote",
                table: "DocumentVersions",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AcquiredAt",
                table: "DocumentEditLocks",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            migrationBuilder.AddColumn<bool>(
                name: "IsReleased",
                table: "DocumentEditLocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
            migrationBuilder.AddColumn<string>(
                name: "LockedByDisplayName",
                table: "DocumentEditLocks",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "LockedByUserId",
                table: "DocumentEditLocks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AllowedExtensions",
                table: "DocumentCategories",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DocumentCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DocumentCategories",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorDisplayName",
                table: "DocumentAuditLogs",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "ActorRole",
                table: "DocumentAuditLogs",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "ActorUserId",
                table: "DocumentAuditLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DocumentAuditLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            migrationBuilder.AddColumn<string>(
                name: "DocumentCode",
                table: "DocumentAuditLogs",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "DocumentAuditLogs",
                type: "TEXT",
                maxLength: 64,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "DocumentAuditLogs",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ProjectDocuments
                SET CreatedByUserId = UploadedById,
                    CurrentApprovalStep = CASE
                        WHEN ApprovalStatus = 1 THEN 1
                        WHEN ApprovalStatus = 2 AND COALESCE(
                            (SELECT ApprovalLevels FROM DocumentCategories WHERE Id = CategoryId), 0) = 2 THEN 2
                        WHEN ApprovalStatus = 2 THEN 1
                        WHEN ApprovalStatus = 3 THEN COALESCE(
                            (SELECT ApprovalLevels FROM DocumentCategories WHERE Id = CategoryId), 0)
                        ELSE 0
                    END,
                    SubmittedByUserId = CASE WHEN ApprovalStatus IN (1, 2, 4) THEN UploadedById ELSE NULL END;

                UPDATE DocumentVersions
                SET CreatedAt = UploadedAt,
                    CreatedByUserId = UploadedById,
                    FileSizeBytes = FileSize,
                    FileExtension = CASE
                        WHEN lower(OriginalFileName) LIKE '%.pdf' THEN '.pdf'
                        WHEN lower(OriginalFileName) LIKE '%.png' THEN '.png'
                        WHEN lower(OriginalFileName) LIKE '%.jpg' THEN '.jpg'
                        WHEN lower(OriginalFileName) LIKE '%.jpeg' THEN '.jpeg'
                        WHEN lower(OriginalFileName) LIKE '%.docx' THEN '.docx'
                        WHEN lower(OriginalFileName) LIKE '%.xlsx' THEN '.xlsx'
                        ELSE ''
                    END;

                UPDATE DocumentEditLocks
                SET AcquiredAt = LockedAt,
                    LockedByUserId = LockedById,
                    LockedByDisplayName = COALESCE(
                        (SELECT FullName FROM AspNetUsers WHERE Id = LockedById),
                        (SELECT UserName FROM AspNetUsers WHERE Id = LockedById),
                        LockedById),
                    IsReleased = CASE WHEN ExpiresAt > CURRENT_TIMESTAMP THEN 0 ELSE 1 END;

                UPDATE DocumentCategories
                SET CreatedAt = '2026-01-01 00:00:00'
                WHERE Id BETWEEN 1 AND 14;

                UPDATE DocumentAuditLogs
                SET ActorUserId = PerformedById,
                    ActorDisplayName = COALESCE(
                        (SELECT FullName FROM AspNetUsers WHERE Id = PerformedById),
                        (SELECT UserName FROM AspNetUsers WHERE Id = PerformedById),
                        PerformedById),
                    ActorRole = 'Legacy',
                    CreatedAt = PerformedAt,
                    DocumentCode = COALESCE(
                        (SELECT DocumentCode FROM ProjectDocuments WHERE Id = DocumentId), '');
                """);

            migrationBuilder.CreateTable(
                name: "DocumentApprovalLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    StepNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ActorUserId = table.Column<string>(type: "TEXT", nullable: false),
                    ActorRole = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CommentOrReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentApprovalLogs", item => item.Id);
                    table.ForeignKey(
                        name: "FK_DocumentApprovalLogs_AspNetUsers_ActorUserId",
                        column: item => item.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentApprovalLogs_DocumentVersions_DocumentVersionId",
                        column: item => item.DocumentVersionId,
                        principalTable: "DocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentApprovalLogs_ProjectDocuments_DocumentId",
                        column: item => item.DocumentId,
                        principalTable: "ProjectDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_CreatedByUserId",
                table: "ProjectDocuments",
                column: "CreatedByUserId");
            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_CreatedByUserId",
                table: "DocumentVersions",
                column: "CreatedByUserId");
            migrationBuilder.CreateIndex(
                name: "IX_DocumentEditLocks_LockedByUserId",
                table: "DocumentEditLocks",
                column: "LockedByUserId");
            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditLogs_ActorUserId",
                table: "DocumentAuditLogs",
                column: "ActorUserId");
            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalLogs_ActorUserId",
                table: "DocumentApprovalLogs",
                column: "ActorUserId");
            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalLogs_DocumentId",
                table: "DocumentApprovalLogs",
                column: "DocumentId");
            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalLogs_DocumentVersionId",
                table: "DocumentApprovalLogs",
                column: "DocumentVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAuditLogs_AspNetUsers_ActorUserId",
                table: "DocumentAuditLogs",
                column: "ActorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAuditLogs_ProjectDocuments_DocumentId",
                table: "DocumentAuditLogs",
                column: "DocumentId",
                principalTable: "ProjectDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_DocumentEditLocks_AspNetUsers_LockedByUserId",
                table: "DocumentEditLocks",
                column: "LockedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_DocumentVersions_AspNetUsers_CreatedByUserId",
                table: "DocumentVersions",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDocuments_AspNetUsers_CreatedByUserId",
                table: "ProjectDocuments",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_DocumentAuditLogs_AspNetUsers_ActorUserId", "DocumentAuditLogs");
            migrationBuilder.DropForeignKey("FK_DocumentAuditLogs_ProjectDocuments_DocumentId", "DocumentAuditLogs");
            migrationBuilder.DropForeignKey("FK_DocumentEditLocks_AspNetUsers_LockedByUserId", "DocumentEditLocks");
            migrationBuilder.DropForeignKey("FK_DocumentVersions_AspNetUsers_CreatedByUserId", "DocumentVersions");
            migrationBuilder.DropForeignKey("FK_ProjectDocuments_AspNetUsers_CreatedByUserId", "ProjectDocuments");
            migrationBuilder.DropTable("DocumentApprovalLogs");

            migrationBuilder.DropIndex("IX_ProjectDocuments_CreatedByUserId", "ProjectDocuments");
            migrationBuilder.DropIndex("IX_DocumentVersions_CreatedByUserId", "DocumentVersions");
            migrationBuilder.DropIndex("IX_DocumentEditLocks_LockedByUserId", "DocumentEditLocks");
            migrationBuilder.DropIndex("IX_DocumentAuditLogs_ActorUserId", "DocumentAuditLogs");

            migrationBuilder.DropColumn("CreatedByUserId", "ProjectDocuments");
            migrationBuilder.DropColumn("CurrentApprovalStep", "ProjectDocuments");
            migrationBuilder.DropColumn("RestoredAt", "ProjectDocuments");
            migrationBuilder.DropColumn("RestoredByUserId", "ProjectDocuments");
            migrationBuilder.DropColumn("SubmittedByUserId", "ProjectDocuments");
            migrationBuilder.DropColumn("UpdatedByUserId", "ProjectDocuments");
            migrationBuilder.DropColumn("CreatedAt", "DocumentVersions");
            migrationBuilder.DropColumn("CreatedByUserId", "DocumentVersions");
            migrationBuilder.DropColumn("FileExtension", "DocumentVersions");
            migrationBuilder.DropColumn("FileSizeBytes", "DocumentVersions");
            migrationBuilder.DropColumn("Sha256Hash", "DocumentVersions");
            migrationBuilder.DropColumn("VersionNote", "DocumentVersions");
            migrationBuilder.DropColumn("AcquiredAt", "DocumentEditLocks");
            migrationBuilder.DropColumn("IsReleased", "DocumentEditLocks");
            migrationBuilder.DropColumn("LockedByDisplayName", "DocumentEditLocks");
            migrationBuilder.DropColumn("LockedByUserId", "DocumentEditLocks");
            migrationBuilder.DropColumn("AllowedExtensions", "DocumentCategories");
            migrationBuilder.DropColumn("CreatedAt", "DocumentCategories");
            migrationBuilder.DropColumn("UpdatedAt", "DocumentCategories");
            migrationBuilder.DropColumn("ActorDisplayName", "DocumentAuditLogs");
            migrationBuilder.DropColumn("ActorRole", "DocumentAuditLogs");
            migrationBuilder.DropColumn("ActorUserId", "DocumentAuditLogs");
            migrationBuilder.DropColumn("CreatedAt", "DocumentAuditLogs");
            migrationBuilder.DropColumn("DocumentCode", "DocumentAuditLogs");
            migrationBuilder.DropColumn("IpAddress", "DocumentAuditLogs");
            migrationBuilder.DropColumn("Reason", "DocumentAuditLogs");
            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAuditLogs_ProjectDocuments_DocumentId",
                table: "DocumentAuditLogs",
                column: "DocumentId",
                principalTable: "ProjectDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
