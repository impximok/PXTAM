using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invexaaa.Migrations
{
    public partial class AddCostingMethodToCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CostingMethod",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 1
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostingMethod",
                table: "Categories"
            );
        }
    }
}
