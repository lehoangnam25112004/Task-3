using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ConstructionProjectManagement.Data;

#nullable disable

namespace ConstructionProjectManagement.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261121144700_Task03FixContractTitle")]
    public partial class Task03FixContractTitle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("-- Contract Title is synchronized by the Task03ModelSync migration.");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("-- Contract Title remains managed by Task03ModelSync.");
        }
    }
}
