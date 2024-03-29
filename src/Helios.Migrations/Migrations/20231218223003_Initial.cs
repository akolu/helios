﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Helios.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
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

            migrationBuilder.CreateTable(
                name: "HouseholdEnergyReadings",
                columns: table => new
                {
                    Time = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Production = table.Column<double>(type: "REAL", nullable: false),
                    Consumption = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdEnergyReadings", x => x.Time);
                });

            migrationBuilder.CreateTable(
                name: "SolarPanelOutputs",
                columns: table => new
                {
                    Time = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Kwh = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolarPanelOutputs", x => x.Time);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectricitySpotPrices");

            migrationBuilder.DropTable(
                name: "HouseholdEnergyReadings");

            migrationBuilder.DropTable(
                name: "SolarPanelOutputs");
        }
    }
}
