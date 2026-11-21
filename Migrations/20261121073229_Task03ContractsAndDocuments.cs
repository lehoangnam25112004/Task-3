using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ConstructionProjectManagement.Migrations
{
    /// <inheritdoc />
    public partial class Task03ContractsAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectDocuments",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "AmendmentId",
                table: "ProjectDocuments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "ProjectDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "ProjectDocuments",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedById",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "ProjectDocuments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentVersionNumber",
                table: "ProjectDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ProjectDocuments",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentCode",
                table: "ProjectDocuments",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemWide",
                table: "ProjectDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegacyCategory",
                table: "ProjectDocuments",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ProjectDocuments",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProjectDocuments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Contracts",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStep",
                table: "Contracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "Contracts",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedById",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientPartyId",
                table: "Contracts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractType",
                table: "Contracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContractorPartyId",
                table: "Contracts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Contracts",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentValue",
                table: "Contracts",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalValue",
                table: "Contracts",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "Contracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Contracts",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedById",
                table: "Contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeOfWork",
                table: "Contracts",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                table: "Contracts",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationName",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractAmendments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmendmentNumber = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    SignedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangeDescription = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ValueAdjustment = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NewExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedById = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedById = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsApplied = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAmendments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAmendments_AspNetUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContractAmendments_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractAmendments_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContractAmendments_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractEditLocks",
                columns: table => new
                {
                    ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                    LockedById = table.Column<string>(type: "TEXT", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractEditLocks", x => x.ContractId);
                    table.ForeignKey(
                        name: "FK_ContractEditLocks_AspNetUsers_LockedById",
                        column: x => x.LockedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractEditLocks_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractParties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    PartyRole = table.Column<int>(type: "INTEGER", nullable: false),
                    TaxCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RepresentativeName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractParties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContractProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    AllocatedValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ScopeNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractProjects_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PerformedById = table.Column<string>(type: "TEXT", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OldValuesJson = table.Column<string>(type: "TEXT", nullable: true),
                    NewValuesJson = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAuditLogs_AspNetUsers_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAuditLogs_ProjectDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "ProjectDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ApprovalLevels = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentEditLocks",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LockedById = table.Column<string>(type: "TEXT", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentEditLocks", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_DocumentEditLocks_AspNetUsers_LockedById",
                        column: x => x.LockedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentEditLocks_ProjectDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "ProjectDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    RelativeStoragePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    ChangeNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ApprovalStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedById = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_ProjectDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "ProjectDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmendmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PerformedById = table.Column<string>(type: "TEXT", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OldValuesJson = table.Column<string>(type: "TEXT", nullable: true),
                    NewValuesJson = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAuditLogs_AspNetUsers_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractAuditLogs_ContractAmendments_AmendmentId",
                        column: x => x.AmendmentId,
                        principalTable: "ContractAmendments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContractAuditLogs_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractValueHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContractId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmendmentId = table.Column<int>(type: "INTEGER", nullable: true),
                    PreviousValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NewValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordedById = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractValueHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractValueHistories_AspNetUsers_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractValueHistories_ContractAmendments_AmendmentId",
                        column: x => x.AmendmentId,
                        principalTable: "ContractAmendments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContractValueHistories_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DocumentCategories",
                columns: new[] { "Id", "ApprovalLevels", "Code", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, 2, "Legal", "Pháp lý & Hành chính", 1, true, "Pháp lý" },
                    { 2, 2, "Contract", "Hợp đồng", 2, true, "Hợp đồng" },
                    { 3, 2, "Amendment", "Hợp đồng", 3, true, "Phụ lục hợp đồng" },
                    { 4, 1, "OfficialLetter", "Hành chính", 4, true, "Công văn" },
                    { 5, 0, "Minutes", "Hành chính", 5, true, "Biên bản họp" },
                    { 6, 2, "Design", "Kỹ thuật", 6, true, "Bản vẽ & Thiết kế" },
                    { 7, 2, "Construction", "Kỹ thuật & Hiện trường", 7, true, "Biện pháp & Thi công" },
                    { 8, 1, "Materials", "Vật tư", 8, true, "Vật tư & Thiết bị" },
                    { 9, 2, "Quality", "Chất lượng", 9, true, "Quản lý chất lượng (QA/QC)" },
                    { 10, 2, "Acceptance", "Chất lượng & Nghiệm thu", 10, true, "Nghiệm thu" },
                    { 11, 1, "Safety", "An toàn", 11, true, "An toàn lao động (HSE)" },
                    { 12, 2, "Payment", "Tài chính", 12, true, "Hồ sơ thanh toán" },
                    { 13, 2, "Handover", "Bàn giao", 13, true, "Bàn giao & Hoàn công" },
                    { 14, 0, "Other", "Chung", 14, true, "Tài liệu khác / Hướng dẫn chung" }
                });

            migrationBuilder.Sql("""
UPDATE ProjectDocuments
SET DocumentCode = 'LEGACY-DOC-' || Id,
    CategoryId = COALESCE((SELECT Id FROM DocumentCategories WHERE Code =
        CASE
            WHEN Category = 'Legal' THEN 'Legal'
            WHEN Category = 'Contract' THEN 'Contract'
            WHEN Category = 'Amendment' THEN 'Amendment'
            WHEN Category = 'OfficialLetter' THEN 'OfficialLetter'
            WHEN Category = 'Minutes' THEN 'Minutes'
            WHEN Category = 'Design' THEN 'Design'
            WHEN Category = 'Construction' THEN 'Construction'
            WHEN Category = 'Materials' THEN 'Materials'
            WHEN Category = 'Quality' THEN 'Quality'
            WHEN Category = 'Acceptance' THEN 'Acceptance'
            WHEN Category = 'Safety' THEN 'Safety'
            WHEN Category = 'Payment' THEN 'Payment'
            WHEN Category = 'Handover' THEN 'Handover'
            ELSE 'Other'
        END), 14),
    LegacyCategory = Category,
    CurrentVersionNumber = 1,
    ApprovalStatus = CASE WHEN IsArchived = 1 THEN 3 ELSE 3 END;

INSERT INTO DocumentVersions
(DocumentId, VersionNumber, OriginalFileName, StoredFileName, RelativeStoragePath, ContentType, FileSize, ChangeNote, ApprovalStatus, UploadedById, UploadedAt, IsCurrent)
SELECT Id, 1, FileName, StoredFileName, 'Documents/legacy/' || StoredFileName, ContentType, FileSize,
       'Migrated from legacy ProjectDocuments', 3, UploadedById, UploadedAt, 1
FROM ProjectDocuments;

UPDATE Contracts
SET OriginalValue = ContractValue,
    CurrentValue = ContractValue,
    EndDate = COALESCE(EndDate, EffectiveDate),
    Currency = 'VND',
    ProgressPercentage = 0,
    ApprovalStep = CASE WHEN Status = 1 THEN 3 ELSE 0 END,
    Status = CASE
        WHEN Status = 0 THEN 0
        WHEN Status = 1 THEN 2
        WHEN Status = 2 THEN 5
        WHEN Status = 3 THEN 5
        ELSE Status
    END;

INSERT INTO ContractProjects (ContractId, ProjectId, AllocatedValue, ScopeNote)
SELECT Id, ProjectId, ContractValue, NULL
FROM Contracts
WHERE ProjectId IS NOT NULL;
""");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_AmendmentId",
                table: "ProjectDocuments",
                column: "AmendmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ApprovedById",
                table: "ProjectDocuments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_ArchivedById",
                table: "ProjectDocuments",
                column: "ArchivedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_CategoryId",
                table: "ProjectDocuments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_DocumentCode",
                table: "ProjectDocuments",
                column: "DocumentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ApprovedById",
                table: "Contracts",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ArchivedById",
                table: "Contracts",
                column: "ArchivedById");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ClientPartyId",
                table: "Contracts",
                column: "ClientPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ContractorPartyId",
                table: "Contracts",
                column: "ContractorPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CreatedById",
                table: "Contracts",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ReviewedById",
                table: "Contracts",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAmendments_AmendmentNumber",
                table: "ContractAmendments",
                column: "AmendmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractAmendments_ApprovedById",
                table: "ContractAmendments",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAmendments_ContractId",
                table: "ContractAmendments",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAmendments_CreatedById",
                table: "ContractAmendments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAmendments_ReviewedById",
                table: "ContractAmendments",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAuditLogs_AmendmentId",
                table: "ContractAuditLogs",
                column: "AmendmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAuditLogs_ContractId",
                table: "ContractAuditLogs",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAuditLogs_PerformedById",
                table: "ContractAuditLogs",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractEditLocks_LockedById",
                table: "ContractEditLocks",
                column: "LockedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContractParties_Code",
                table: "ContractParties",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractProjects_ContractId_ProjectId",
                table: "ContractProjects",
                columns: new[] { "ContractId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractProjects_ProjectId",
                table: "ContractProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractValueHistories_AmendmentId",
                table: "ContractValueHistories",
                column: "AmendmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractValueHistories_ContractId",
                table: "ContractValueHistories",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractValueHistories_RecordedById",
                table: "ContractValueHistories",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditLogs_DocumentId",
                table: "DocumentAuditLogs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAuditLogs_PerformedById",
                table: "DocumentAuditLogs",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategories_Code",
                table: "DocumentCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentEditLocks_LockedById",
                table: "DocumentEditLocks",
                column: "LockedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId_VersionNumber",
                table: "DocumentVersions",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_UploadedById",
                table: "DocumentVersions",
                column: "UploadedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_AspNetUsers_ApprovedById",
                table: "Contracts",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_AspNetUsers_ArchivedById",
                table: "Contracts",
                column: "ArchivedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_AspNetUsers_CreatedById",
                table: "Contracts",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_AspNetUsers_ReviewedById",
                table: "Contracts",
                column: "ReviewedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_ContractParties_ClientPartyId",
                table: "Contracts",
                column: "ClientPartyId",
                principalTable: "ContractParties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_ContractParties_ContractorPartyId",
                table: "Contracts",
                column: "ContractorPartyId",
                principalTable: "ContractParties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDocuments_AspNetUsers_ApprovedById",
                table: "ProjectDocuments",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDocuments_AspNetUsers_ArchivedById",
                table: "ProjectDocuments",
                column: "ArchivedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDocuments_ContractAmendments_AmendmentId",
                table: "ProjectDocuments",
                column: "AmendmentId",
                principalTable: "ContractAmendments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDocuments_DocumentCategories_CategoryId",
                table: "ProjectDocuments",
                column: "CategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_AspNetUsers_ApprovedById",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_AspNetUsers_ArchivedById",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_AspNetUsers_CreatedById",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_AspNetUsers_ReviewedById",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_ContractParties_ClientPartyId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_ContractParties_ContractorPartyId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDocuments_AspNetUsers_ApprovedById",
                table: "ProjectDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDocuments_AspNetUsers_ArchivedById",
                table: "ProjectDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDocuments_ContractAmendments_AmendmentId",
                table: "ProjectDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDocuments_DocumentCategories_CategoryId",
                table: "ProjectDocuments");

            migrationBuilder.DropTable(
                name: "ContractAuditLogs");

            migrationBuilder.DropTable(
                name: "ContractEditLocks");

            migrationBuilder.DropTable(
                name: "ContractParties");

            migrationBuilder.DropTable(
                name: "ContractProjects");

            migrationBuilder.DropTable(
                name: "ContractValueHistories");

            migrationBuilder.DropTable(
                name: "DocumentAuditLogs");

            migrationBuilder.DropTable(
                name: "DocumentCategories");

            migrationBuilder.DropTable(
                name: "DocumentEditLocks");

            migrationBuilder.DropTable(
                name: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "ContractAmendments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDocuments_AmendmentId",
                table: "ProjectDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDocuments_ApprovedById",
                table: "ProjectDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDocuments_ArchivedById",
                table: "ProjectDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDocuments_CategoryId",
                table: "ProjectDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDocuments_DocumentCode",
                table: "ProjectDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ApprovedById",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ArchivedById",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ClientPartyId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ContractorPartyId",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_CreatedById",
                table: "Contracts");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_ReviewedById",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "AmendmentId",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "CurrentVersionNumber",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentCode",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "IsSystemWide",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "LegacyCategory",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProjectDocuments");

            migrationBuilder.DropColumn(
                name: "ApprovalStep",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ArchivedById",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ClientPartyId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ContractorPartyId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "CurrentValue",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "OriginalValue",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "ScopeOfWork",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "StatusReason",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "OrganizationName",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Contracts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
