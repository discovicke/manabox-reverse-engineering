using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MtgBulk.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScryfallId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SetCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    SetName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CollectorNumber = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Rarity = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Foil = table.Column<bool>(type: "INTEGER", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    PriceEur = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    PriceUsd = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    ImageSmall = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ImageNormal = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ScryfallUri = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    RawInput = table.Column<string>(type: "TEXT", nullable: true),
                    ManaBoxId = table.Column<int>(type: "INTEGER", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ScryfallId",
                table: "Cards",
                column: "ScryfallId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cards");
        }
    }
}
