using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invexaaa.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitCostToStockTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "StockTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "StockTransactions");
        }
    }
}
