using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invexaaa.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStockBatchAndAddConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchStatus",
                table: "StockBatches");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StockBatches",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Inventories",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StockBatches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Inventories");

            migrationBuilder.AddColumn<string>(
                name: "BatchStatus",
                table: "StockBatches",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
