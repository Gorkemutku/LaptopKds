using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaptopKDS.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecisionProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IconEmoji = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Laptops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CpuScore = table.Column<double>(type: "REAL", nullable: false),
                    GpuScore = table.Column<double>(type: "REAL", nullable: false),
                    RamCapacityGB = table.Column<double>(type: "REAL", nullable: false),
                    WeightKG = table.Column<double>(type: "REAL", nullable: false),
                    BatteryLifeHours = table.Column<double>(type: "REAL", nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laptops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileCriterionWeights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightPrice = table.Column<double>(type: "REAL", nullable: false),
                    WeightCpuScore = table.Column<double>(type: "REAL", nullable: false),
                    WeightGpuScore = table.Column<double>(type: "REAL", nullable: false),
                    WeightRamCapacity = table.Column<double>(type: "REAL", nullable: false),
                    WeightKG = table.Column<double>(type: "REAL", nullable: false),
                    WeightBattery = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileCriterionWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileCriterionWeights_DecisionProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "DecisionProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileCriterionWeights_ProfileId",
                table: "ProfileCriterionWeights",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Laptops");

            migrationBuilder.DropTable(
                name: "ProfileCriterionWeights");

            migrationBuilder.DropTable(
                name: "DecisionProfiles");
        }
    }
}
