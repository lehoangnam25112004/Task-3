using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ConstructionProjectManagement.Migrations
{
    /// <inheritdoc />
    public partial class Task02Phase1ProjectMemberAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectManagerHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousProjectManagerId = table.Column<string>(type: "TEXT", nullable: true),
                    NewProjectManagerId = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ChangeReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectManagerHistories", x => x.Id);
                    table.CheckConstraint("CK_ProjectManagerHistories_DateRange", "\"StartedAt\" IS NULL OR \"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\"");
                    table.ForeignKey(
                        name: "FK_ProjectManagerHistories_AspNetUsers_NewProjectManagerId",
                        column: x => x.NewProjectManagerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectManagerHistories_AspNetUsers_PreviousProjectManagerId",
                        column: x => x.PreviousProjectManagerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectManagerHistories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRoles",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MaximumConcurrentProjects = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRoles", x => x.Code);
                    table.CheckConstraint("CK_ProjectRoles_MaximumConcurrentProjects", "\"MaximumConcurrentProjects\" IS NULL OR \"MaximumConcurrentProjects\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "ProjectMemberRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectMemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectRoleCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMemberRoleAssignments", x => x.Id);
                    table.CheckConstraint("CK_ProjectMemberRoleAssignments_DateRange", "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\"");
                    table.ForeignKey(
                        name: "FK_ProjectMemberRoleAssignments_ProjectMembers_ProjectMemberId",
                        column: x => x.ProjectMemberId,
                        principalTable: "ProjectMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMemberRoleAssignments_ProjectRoles_ProjectRoleCode",
                        column: x => x.ProjectRoleCode,
                        principalTable: "ProjectRoles",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ProjectRoles",
                columns: new[] { "Code", "MaximumConcurrentProjects", "Name" },
                values: new object[,]
                {
                    { "DEPUTY_PROJECT_MANAGER", null, "Deputy Project Manager" },
                    { "PROJECT_MANAGER", null, "Project Manager" },
                    { "SITE_MANAGER", 1, "Site Manager" },
                    { "SUPERVISING_ENGINEER", 1, "Supervising Engineer" }
                });

            migrationBuilder.Sql("""
                ALTER TABLE "ProjectMembers"
                ADD COLUMN "EmployeeId" INTEGER NULL
                REFERENCES "Employees" ("Id") ON DELETE RESTRICT;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_EmployeeId",
                table: "ProjectMembers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_EmployeeId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "EmployeeId" },
                unique: true,
                filter: "\"EmployeeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectManagerHistories_NewProjectManagerId",
                table: "ProjectManagerHistories",
                column: "NewProjectManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectManagerHistories_PreviousProjectManagerId",
                table: "ProjectManagerHistories",
                column: "PreviousProjectManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectManagerHistories_ProjectId",
                table: "ProjectManagerHistories",
                column: "ProjectId",
                unique: true,
                filter: "\"EndedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberRoleAssignments_ProjectMemberId_ProjectRoleCode",
                table: "ProjectMemberRoleAssignments",
                columns: new[] { "ProjectMemberId", "ProjectRoleCode" },
                unique: true,
                filter: "\"EndedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMemberRoleAssignments_ProjectRoleCode",
                table: "ProjectMemberRoleAssignments",
                column: "ProjectRoleCode");

            migrationBuilder.Sql("""
                UPDATE "ProjectMembers"
                SET "EmployeeId" = (
                    SELECT "Employees"."Id"
                    FROM "Employees"
                    WHERE "Employees"."UserId" = "ProjectMembers"."UserId")
                WHERE "EmployeeId" IS NULL
                  AND EXISTS (
                    SELECT 1
                    FROM "Employees"
                    WHERE "Employees"."UserId" = "ProjectMembers"."UserId");
                """);

            migrationBuilder.Sql("""
                INSERT INTO "ProjectManagerHistories"
                    ("ProjectId", "PreviousProjectManagerId", "NewProjectManagerId",
                     "StartedAt", "EndedAt", "ChangeReason")
                SELECT "Id", NULL, "ProjectManagerId", NULL, NULL,
                       'Baseline state at migration; start time unknown'
                FROM "Projects";
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_Projects_PMHistory_Insert"
                AFTER INSERT ON "Projects"
                BEGIN
                    INSERT INTO "ProjectManagerHistories"
                        ("ProjectId", "PreviousProjectManagerId", "NewProjectManagerId",
                         "StartedAt", "EndedAt", "ChangeReason")
                    VALUES
                        (NEW."Id", NULL, NEW."ProjectManagerId",
                         strftime('%Y-%m-%d %H:%M:%f', 'now'), NULL, NULL);
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_Projects_PMHistory_Update"
                AFTER UPDATE OF "ProjectManagerId" ON "Projects"
                WHEN OLD."ProjectManagerId" IS NOT NEW."ProjectManagerId"
                BEGIN
                    UPDATE "ProjectManagerHistories"
                    SET "EndedAt" = strftime('%Y-%m-%d %H:%M:%f', 'now')
                    WHERE "ProjectId" = NEW."Id" AND "EndedAt" IS NULL;

                    INSERT INTO "ProjectManagerHistories"
                        ("ProjectId", "PreviousProjectManagerId", "NewProjectManagerId",
                         "StartedAt", "EndedAt", "ChangeReason")
                    VALUES
                        (NEW."Id", OLD."ProjectManagerId", NEW."ProjectManagerId",
                         strftime('%Y-%m-%d %H:%M:%f', 'now'), NULL, NULL);
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_ProjectMemberRoleAssignments_ValidateInsert"
                BEFORE INSERT ON "ProjectMemberRoleAssignments"
                WHEN NEW."EndedAt" IS NULL
                BEGIN
                    SELECT RAISE(ABORT, 'Active role assignment requires an active ProjectMember linked to an Employee')
                    WHERE NOT EXISTS (
                        SELECT 1 FROM "ProjectMembers"
                        WHERE "Id" = NEW."ProjectMemberId"
                          AND "EmployeeId" IS NOT NULL
                          AND "IsActive" = 1);

                    SELECT RAISE(ABORT, 'Role exceeds its maximum concurrent project count')
                    WHERE (
                        SELECT "MaximumConcurrentProjects"
                        FROM "ProjectRoles"
                        WHERE "Code" = NEW."ProjectRoleCode") IS NOT NULL
                      AND (
                        SELECT COUNT(DISTINCT ExistingMember."ProjectId")
                        FROM "ProjectMemberRoleAssignments" ExistingAssignment
                        JOIN "ProjectMembers" ExistingMember
                          ON ExistingMember."Id" = ExistingAssignment."ProjectMemberId"
                        JOIN "ProjectMembers" CandidateMember
                          ON CandidateMember."Id" = NEW."ProjectMemberId"
                        WHERE ExistingMember."EmployeeId" = CandidateMember."EmployeeId"
                          AND ExistingAssignment."ProjectRoleCode" = NEW."ProjectRoleCode"
                          AND ExistingAssignment."EndedAt" IS NULL
                      ) >= (
                        SELECT "MaximumConcurrentProjects"
                        FROM "ProjectRoles"
                        WHERE "Code" = NEW."ProjectRoleCode");
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_ProjectMemberRoleAssignments_ValidateUpdate"
                BEFORE UPDATE OF "ProjectMemberId", "ProjectRoleCode", "EndedAt"
                ON "ProjectMemberRoleAssignments"
                WHEN NEW."EndedAt" IS NULL
                BEGIN
                    SELECT RAISE(ABORT, 'Active role assignment requires an active ProjectMember linked to an Employee')
                    WHERE NOT EXISTS (
                        SELECT 1 FROM "ProjectMembers"
                        WHERE "Id" = NEW."ProjectMemberId"
                          AND "EmployeeId" IS NOT NULL
                          AND "IsActive" = 1);

                    SELECT RAISE(ABORT, 'Role exceeds its maximum concurrent project count')
                    WHERE (
                        SELECT "MaximumConcurrentProjects"
                        FROM "ProjectRoles"
                        WHERE "Code" = NEW."ProjectRoleCode") IS NOT NULL
                      AND (
                        SELECT COUNT(DISTINCT ExistingMember."ProjectId")
                        FROM "ProjectMemberRoleAssignments" ExistingAssignment
                        JOIN "ProjectMembers" ExistingMember
                          ON ExistingMember."Id" = ExistingAssignment."ProjectMemberId"
                        JOIN "ProjectMembers" CandidateMember
                          ON CandidateMember."Id" = NEW."ProjectMemberId"
                        WHERE ExistingMember."EmployeeId" = CandidateMember."EmployeeId"
                          AND ExistingAssignment."ProjectRoleCode" = NEW."ProjectRoleCode"
                          AND ExistingAssignment."EndedAt" IS NULL
                          AND ExistingAssignment."Id" <> NEW."Id"
                      ) >= (
                        SELECT "MaximumConcurrentProjects"
                        FROM "ProjectRoles"
                        WHERE "Code" = NEW."ProjectRoleCode");
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_ProjectMembers_PreventInactivationWithRoles"
                BEFORE UPDATE OF "IsActive" ON "ProjectMembers"
                WHEN NEW."IsActive" = 0 AND EXISTS (
                    SELECT 1 FROM "ProjectMemberRoleAssignments"
                    WHERE "ProjectMemberId" = OLD."Id" AND "EndedAt" IS NULL)
                BEGIN
                    SELECT RAISE(ABORT, 'End active role assignments before inactivating a ProjectMember');
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER "TR_ProjectMembers_PreventReassignmentWithRoles"
                BEFORE UPDATE OF "ProjectId", "EmployeeId" ON "ProjectMembers"
                WHEN EXISTS (
                    SELECT 1 FROM "ProjectMemberRoleAssignments"
                    WHERE "ProjectMemberId" = OLD."Id" AND "EndedAt" IS NULL)
                BEGIN
                    SELECT RAISE(ABORT, 'Project or Employee cannot change while active role assignments exist');
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS \"TR_ProjectMembers_PreventReassignmentWithRoles\";");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS \"TR_ProjectMembers_PreventInactivationWithRoles\";");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS \"TR_ProjectMemberRoleAssignments_ValidateUpdate\";");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS \"TR_ProjectMemberRoleAssignments_ValidateInsert\";");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS \"TR_Projects_PMHistory_Update\";");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS \"TR_Projects_PMHistory_Insert\";");

            migrationBuilder.DropTable(
                name: "ProjectManagerHistories");

            migrationBuilder.DropTable(
                name: "ProjectMemberRoleAssignments");

            migrationBuilder.DropTable(
                name: "ProjectRoles");

            migrationBuilder.Sql("""
                CREATE TEMP TABLE "__Task02ProjectMemberSequence" ("Sequence" INTEGER NOT NULL);
                INSERT INTO "__Task02ProjectMemberSequence" ("Sequence")
                SELECT COALESCE(
                    (SELECT "seq" FROM "sqlite_sequence" WHERE "name" = 'ProjectMembers'), 0);

                CREATE TABLE "ef_temp_ProjectMembers" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ProjectMembers" PRIMARY KEY AUTOINCREMENT,
                    "ProjectId" INTEGER NOT NULL,
                    "UserId" TEXT NOT NULL,
                    "AssignmentRole" TEXT NULL,
                    "JoinedAt" TEXT NOT NULL,
                    "IsActive" INTEGER NOT NULL,
                    CONSTRAINT "FK_ProjectMembers_AspNetUsers_UserId"
                        FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_ProjectMembers_Projects_ProjectId"
                        FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
                );

                INSERT INTO "ef_temp_ProjectMembers"
                    ("Id", "ProjectId", "UserId", "AssignmentRole", "JoinedAt", "IsActive")
                SELECT "Id", "ProjectId", "UserId", "AssignmentRole", "JoinedAt", "IsActive"
                FROM "ProjectMembers";

                DROP TABLE "ProjectMembers";
                ALTER TABLE "ef_temp_ProjectMembers" RENAME TO "ProjectMembers";

                CREATE INDEX "IX_ProjectMembers_UserId" ON "ProjectMembers" ("UserId");
                CREATE UNIQUE INDEX "IX_ProjectMembers_ProjectId_UserId"
                    ON "ProjectMembers" ("ProjectId", "UserId");

                UPDATE "sqlite_sequence"
                SET "seq" = MAX("seq", (SELECT "Sequence" FROM "__Task02ProjectMemberSequence"))
                WHERE "name" = 'ProjectMembers';
                INSERT INTO "sqlite_sequence" ("name", "seq")
                SELECT 'ProjectMembers', "Sequence"
                FROM "__Task02ProjectMemberSequence"
                WHERE NOT EXISTS (
                    SELECT 1 FROM "sqlite_sequence" WHERE "name" = 'ProjectMembers');
                DROP TABLE "__Task02ProjectMemberSequence";
                """);

        }
    }
}
