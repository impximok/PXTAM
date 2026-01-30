using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invexaaa.Migrations
{
    /// <inheritdoc />
    public partial class StockCostingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                table: "StockBatches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SupplierID",
                table: "StockBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierNameSnapshot",
                table: "StockBatches",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TransactionUnitCost",
                table: "StockBatches",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CostingMethod",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageUnitCost",
                table: "Inventories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCostUpdated",
                table: "Inventories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalStockValue",
                table: "Inventories",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "SupplierID",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "SupplierNameSnapshot",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "TransactionUnitCost",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "CostingMethod",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "AverageUnitCost",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "LastCostUpdated",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "TotalStockValue",
                table: "Inventories");
        }
    }
}
