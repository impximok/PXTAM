using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SnomiAssignmentReal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategoryDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CustomerFullName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerUserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerPasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CustomerProfileImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsCustomerLoggedIn = table.Column<bool>(type: "bit", nullable: false),
                    CustomerEmailAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerPhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    CustomerRewardPoints = table.Column<int>(type: "int", nullable: false),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "Functions",
                columns: table => new
                {
                    FunctionId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FunctionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FunctionDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Functions", x => x.FunctionId);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserRoleId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoleDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.UserRoleId);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    MenuItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MenuItemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MenuItemDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MenuItemCalories = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MenuItemUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MenuItemImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAvailableForOrder = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.MenuItemId);
                    table.ForeignKey(
                        name: "FK_MenuItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerOrders",
                columns: table => new
                {
                    CustomerOrderId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CustomerId = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    TableNumber = table.Column<int>(type: "int", nullable: false),
                    OrderCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentMethodName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrderTotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RewardPointsRedeemed = table.Column<int>(type: "int", nullable: false),
                    TotalDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RewardPointsEarned = table.Column<int>(type: "int", nullable: false),
                    NetPayableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RewardPointsAwardedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailReceiptSentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrders", x => x.CustomerOrderId);
                    table.ForeignKey(
                        name: "FK_CustomerOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleFunctions",
                columns: table => new
                {
                    UserRoleFunctionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserRoleId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FunctionId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsFunctionEnabledForRole = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleFunctions", x => x.UserRoleFunctionId);
                    table.ForeignKey(
                        name: "FK_UserRoleFunctions_Functions_FunctionId",
                        column: x => x.FunctionId,
                        principalTable: "Functions",
                        principalColumn: "FunctionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleFunctions_UserRoles_UserRoleId",
                        column: x => x.UserRoleId,
                        principalTable: "UserRoles",
                        principalColumn: "UserRoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UserFullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserProfileImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LoginEmailAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HashedPassword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserRoleId = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_UserRoles_UserRoleId",
                        column: x => x.UserRoleId,
                        principalTable: "UserRoles",
                        principalColumn: "UserRoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    OrderDetailId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CustomerOrderId = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    OrderedQuantity = table.Column<int>(type: "int", nullable: false),
                    MenuItemId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.OrderDetailId);
                    table.ForeignKey(
                        name: "FK_OrderDetails_CustomerOrders_CustomerOrderId",
                        column: x => x.CustomerOrderId,
                        principalTable: "CustomerOrders",
                        principalColumn: "CustomerOrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetails_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "MenuItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderCustomizationSettings",
                columns: table => new
                {
                    MenuItemCustomizationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CustomizationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomizationDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CustomizationAdditionalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EligibleCategoryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MenuItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderDetailId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCustomizationSettings", x => x.MenuItemCustomizationId);
                    table.ForeignKey(
                        name: "FK_OrderCustomizationSettings_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "MenuItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderCustomizationSettings_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "OrderDetailId");
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryDescription", "CategoryName" },
                values: new object[,]
                {
                    { "C100", "Hearty breakfast options served all day", "All-Day Breakfast" },
                    { "C101", "Artisan sandwiches and cheesy toasties", "Sandwiches & Toasties" },
                    { "C102", "Fresh salads and nutrient-rich grain bowls", "Salads & Healthy Bowls" },
                    { "C103", "House-made cakes, tarts, and sweet treats", "Signature Desserts" },
                    { "C104", "Hot and iced classic café beverages", "Coffee & Tea" },
                    { "C105", "	Fruity smoothies, milkshakes, and fizzy mocktails", "Smoothies & Coolers" },
                    { "C106", "Freshly baked croissants, muffins, and pastries", "Baked Goods & Pastries" },
                    { "C107", "Limited-time festive or themed meal sets", "Seasonal Specials" }
                });

            migrationBuilder.InsertData(
                table: "Functions",
                columns: new[] { "FunctionId", "FunctionDescription", "FunctionName" },
                values: new object[,]
                {
                    { "F100", "Add, edit, delete menu items", "Manage Menu" },
                    { "F101", "View and change status of orders", "View CustomerOrders" },
                    { "F102", "Access past orders", "Order History" },
                    { "F103", "Access report", "View Report" },
                    { "F104", "Manage accounts", "Manage accounts" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "UserRoleId", "RoleDescription", "RoleName" },
                values: new object[,]
                {
                    { "UR100", "System Administrator", "Admin" },
                    { "UR101", "Cafe Staff", "Staff" }
                });

            migrationBuilder.InsertData(
                table: "UserRoleFunctions",
                columns: new[] { "UserRoleFunctionId", "FunctionId", "IsFunctionEnabledForRole", "UserRoleId" },
                values: new object[,]
                {
                    { "URF100", "F100", true, "UR100" },
                    { "URF101", "F101", true, "UR100" },
                    { "URF102", "F102", true, "UR100" },
                    { "URF103", "F103", true, "UR100" },
                    { "URF104", "F104", true, "UR100" },
                    { "URF200", "F101", true, "UR101" },
                    { "URF201", "F102", true, "UR101" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_CustomerId",
                table: "CustomerOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId",
                table: "MenuItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCustomizationSettings_MenuItemId",
                table: "OrderCustomizationSettings",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderCustomizationSettings_OrderDetailId",
                table: "OrderCustomizationSettings",
                column: "OrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_CustomerOrderId",
                table: "OrderDetails",
                column: "CustomerOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_MenuItemId",
                table: "OrderDetails",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleFunctions_FunctionId",
                table: "UserRoleFunctions",
                column: "FunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleFunctions_UserRoleId",
                table: "UserRoleFunctions",
                column: "UserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserRoleId",
                table: "Users",
                column: "UserRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderCustomizationSettings");

            migrationBuilder.DropTable(
                name: "UserRoleFunctions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "Functions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "CustomerOrders");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
