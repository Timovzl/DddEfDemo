using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(28,0)", precision: 28, scale: 0, nullable: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModificationDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    ManufacturerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true, collation: "Latin1_General_100_CI_AS"),
                    ManufacturerEst = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotations",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(28,0)", precision: 28, scale: 0, nullable: false),
                    SellerId = table.Column<decimal>(type: "decimal(28,0)", precision: 28, scale: 0, nullable: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ExpirationDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Lines = table.Column<string>(type: "nvarchar(max)", nullable: false, collation: "Latin1_General_100_BIN2"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sellers",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "decimal(28,0)", precision: 28, scale: 0, nullable: false),
                    CreationDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModificationDateTime = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Latin1_General_100_CI_AS"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sellers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "ManufacturerEst", "ManufacturerName", "CreationDateTime", "ModificationDateTime", "Name" },
                values: new object[] { 1m, 1970, "Bob the Builder", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Product One" });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CreationDateTime", "ModificationDateTime", "Name" },
                values: new object[] { 2m, new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Product Two" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreationDateTime",
                table: "Products",
                column: "CreationDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ModificationDateTime",
                table: "Products",
                column: "ModificationDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_CreationDateTime",
                table: "Quotations",
                column: "CreationDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_ExpirationDateTime",
                table: "Quotations",
                column: "ExpirationDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_CreationDateTime",
                table: "Sellers",
                column: "CreationDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_ModificationDateTime",
                table: "Sellers",
                column: "ModificationDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Quotations");

            migrationBuilder.DropTable(
                name: "Sellers");
        }
    }
}
