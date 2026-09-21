using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyRental.Web.Data.Migrations
{
    public partial class TrackDemoSeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeedHistory",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_SeedHistory", x => x.Name));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SeedHistory");
        }
    }
}
