using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnomiAssignmentReal.Migrations
{
    public partial class UpdatePaymentColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop default constraint if exists
            migrationBuilder.Sql(@"
DECLARE @constraint NVARCHAR(200);
SELECT @constraint = name 
FROM sys.default_constraints 
WHERE parent_object_id = OBJECT_ID('CustomerOrders')
AND parent_column_id = (
    SELECT column_id FROM sys.columns 
    WHERE name = 'PaymentCompletedAt' 
      AND object_id = OBJECT_ID('CustomerOrders')
);

IF @constraint IS NOT NULL
    EXEC('ALTER TABLE CustomerOrders DROP CONSTRAINT ' + @constraint);
");

            // 2. Make nullable
            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentCompletedAt",
                table: "CustomerOrders",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentCompletedAt",
                table: "CustomerOrders",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldNullable: true
            );
        }
    }
}
