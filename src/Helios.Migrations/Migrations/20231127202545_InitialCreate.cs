using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helios.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnergyMeasurements",
                columns: table => new
                {
                    Time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FlowType = table.Column<int>(type: "INTEGER", nullable: false),
                    Kwh = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyMeasurements", x => new { x.Time, x.FlowType });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnergyMeasurements");
        }
    }
}
