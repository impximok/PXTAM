using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invexaaa.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSupplierFromItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierID",
                table: "Items");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "StockTransactions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "StockTransactions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<int>(
                name: "SupplierID",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
