using Microsoft.EntityFrameworkCore;
using SnomiAssignmentReal.Models;

namespace SnomiAssignmentReal.Data;

public class ApplicationDbContext : DbContext
{
    // Constructor
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Add DbSets for each entity class
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerOrder> CustomerOrders { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<OrderCustomizationSettings> OrderCustomizationSettings { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Function> Functions { get; set; }
    public DbSet<UserRoleFunction> UserRoleFunctions { get; set; }
   



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Seed User Roles
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole { UserRoleId = "UR100", RoleName = "Admin", RoleDescription = "System Administrator" },
            new UserRole { UserRoleId = "UR101", RoleName = "Staff", RoleDescription = "Cafe Staff" }
        );

        // 2. Seed Functions
        modelBuilder.Entity<Function>().HasData(
            new Function { FunctionId = "F100", FunctionName = "Manage Menu", FunctionDescription = "Add, edit, delete menu items" },
            new Function { FunctionId = "F101", FunctionName = "View CustomerOrders", FunctionDescription = "View and change status of orders" },
            new Function { FunctionId = "F102", FunctionName = "Order History", FunctionDescription = "Access past orders" },
            new Function { FunctionId = "F103", FunctionName = "View Report", FunctionDescription = "Access report" },
            new Function { FunctionId = "F104", FunctionName = "Manage accounts", FunctionDescription = "Manage accounts" }
        );

        // 3. Seed FunctionRoleMappings (linking roles to functions)
        modelBuilder.Entity<UserRoleFunction>().HasData(
            new UserRoleFunction { UserRoleFunctionId = "URF100", UserRoleId = "UR100", FunctionId = "F100", IsFunctionEnabledForRole = true }, // Admin
            new UserRoleFunction { UserRoleFunctionId = "URF101", UserRoleId = "UR100", FunctionId = "F101", IsFunctionEnabledForRole = true },
            new UserRoleFunction { UserRoleFunctionId = "URF102", UserRoleId = "UR100", FunctionId = "F102", IsFunctionEnabledForRole = true },
            new UserRoleFunction { UserRoleFunctionId = "URF103", UserRoleId = "UR100", FunctionId = "F103", IsFunctionEnabledForRole = true },
            new UserRoleFunction { UserRoleFunctionId = "URF104", UserRoleId = "UR100", FunctionId = "F104", IsFunctionEnabledForRole = true },

            new UserRoleFunction { UserRoleFunctionId = "URF200", UserRoleId = "UR101", FunctionId = "F101", IsFunctionEnabledForRole = true }, // Staff
            new UserRoleFunction { UserRoleFunctionId = "URF201", UserRoleId = "UR101", FunctionId = "F102", IsFunctionEnabledForRole = true }
        );

        // 4. Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = "C100", CategoryName = "All-Day Breakfast", CategoryDescription = "Hearty breakfast options served all day" },
            new Category { CategoryId = "C101", CategoryName = "Sandwiches & Toasties", CategoryDescription = "Artisan sandwiches and cheesy toasties" },
            new Category { CategoryId = "C102", CategoryName = "Salads & Healthy Bowls", CategoryDescription = "Fresh salads and nutrient-rich grain bowls" },
            new Category { CategoryId = "C103", CategoryName = "Signature Desserts", CategoryDescription = "House-made cakes, tarts, and sweet treats" },
            new Category { CategoryId = "C104", CategoryName = "Coffee & Tea", CategoryDescription = "Hot and iced classic café beverages" },
            new Category { CategoryId = "C105", CategoryName = "Smoothies & Coolers", CategoryDescription = "\tFruity smoothies, milkshakes, and fizzy mocktails" },
            new Category { CategoryId = "C106", CategoryName = "Baked Goods & Pastries", CategoryDescription = "Freshly baked croissants, muffins, and pastries" },
            new Category { CategoryId = "C107", CategoryName = "Seasonal Specials", CategoryDescription = "Limited-time festive or themed meal sets" }

        );

       




    }
}



