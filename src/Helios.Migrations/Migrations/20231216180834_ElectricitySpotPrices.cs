using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helios.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ElectricitySpotPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElectricitySpotPrices",
                columns: table => new
                {
                    Time = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EuroCentsPerKWh = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricitySpotPrices", x => x.Time);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectricitySpotPrices");
        }
    }
}
