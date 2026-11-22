using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConstructionProjectManagement.Migrations
{
    /// <inheritdoc />
    public partial class Task04WbsLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "WbsCode",
                table: "WorkItems",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchiveGroupId",
                table: "WorkItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "WorkItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "WorkItems",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "WorkItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "WorkItems",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "WorkItems",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "WorkItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StatusBeforeArchive",
                table: "WorkItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "WorkItems",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchiveGroupId",
                table: "ProjectPhases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "ProjectPhases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivedByUserId",
                table: "ProjectPhases",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "ProjectPhases",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StatusBeforeArchive",
                table: "ProjectPhases",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "ProjectPhases"
                SET
                    "Status" = CASE
                        WHEN "Status" IS NULL OR trim("Status") = '' THEN '0'
                        WHEN lower(trim("Status")) = 'planned' THEN '0'
                        WHEN lower(trim("Status")) IN ('inprogress', 'in progress', 'active') THEN '1'
                        WHEN lower(trim("Status")) IN ('completed', 'done') THEN '2'
                        WHEN lower(trim("Status")) = 'archived' THEN '3'
                        ELSE '0'
                    END,
                    "IsArchived" = CASE
                        WHEN lower(trim(COALESCE("Status", ''))) = 'archived' THEN 1
                        ELSE 0
                    END,
                    "StatusBeforeArchive" = CASE
                        WHEN lower(trim(COALESCE("Status", ''))) = 'archived' THEN 0
                        ELSE NULL
                    END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "ProjectPhases",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ProjectId_IsArchived",
                table: "WorkItems",
                columns: new[] { "ProjectId", "IsArchived" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkItems_Level",
                table: "WorkItems",
                sql: "\"Level\" >= 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPhases_ProjectId_IsArchived",
                table: "ProjectPhases",
                columns: new[] { "ProjectId", "IsArchived" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_ProjectId_IsArchived",
                table: "WorkItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkItems_Level",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_ProjectPhases_ProjectId_IsArchived",
                table: "ProjectPhases");

            migrationBuilder.DropColumn(
                name: "ArchiveGroupId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "StatusBeforeArchive",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "ArchiveGroupId",
                table: "ProjectPhases");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ProjectPhases");

            migrationBuilder.DropColumn(
                name: "ArchivedByUserId",
                table: "ProjectPhases");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ProjectPhases");

            migrationBuilder.DropColumn(
                name: "StatusBeforeArchive",
                table: "ProjectPhases");

            migrationBuilder.Sql(
                """
                UPDATE "ProjectPhases"
                SET "Status" = CASE "Status"
                    WHEN 0 THEN 'Planned'
                    WHEN 1 THEN 'InProgress'
                    WHEN 2 THEN 'Completed'
                    WHEN 3 THEN 'Archived'
                    ELSE 'Planned'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "WbsCode",
                table: "WorkItems",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProjectPhases",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }
    }
}
