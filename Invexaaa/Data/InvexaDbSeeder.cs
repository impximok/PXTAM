using System;
using System.Linq;
using Invexaaa.Models.Invexa;
using Invexaaa.Helpers;

namespace Invexaaa.Data
{
    public static class InvexaDbSeeder
    {
        public static void Seed(InvexaDbContext context)
        {
            // ===============================
            // USERS (seed once)
            // ===============================
            if (!context.Users.Any())
            {
                SeedUsers(context);
            }

            // ===============================
            // MULTI-UOM (seed once)
            // ===============================
            SeedItemUnitConversions(context);
        }

        // =====================================================
        // USERS
        // =====================================================
        private static void SeedUsers(InvexaDbContext context)
        {
            UpsertUser(
                context,
                "emilycarter@invexa.com",
                "Emily Carter",
                "0123456789",
                "Admin",
                "Emily@123",
                "/images/users/Admin.jpg"
            );

            UpsertUser(
                context,
                "impximok@gmail.com",
                "impximok",
                "0123456791",
                "Admin",
                "Impximok@123",
                "/images/users/Admin.jpg"
            );

            UpsertUser(
                context,
                "sophiawilliams@invexa.com",
                "Sophia Williams",
                "0123456790",
                "Manager",
                "Sophia@123",
                "/images/users/Manager.jpg"
            );

            UpsertUser(
                context,
                "danielthompson@invexa.com",
                "Daniel Thompson",
                "0123456791",
                "Staff",
                "Daniel@123",
                "/images/users/Staff.jpg"
            );

            context.SaveChanges();
        }

        // =====================================================
        // INSERT IF NOT EXISTS, UPDATE SAFELY IF EXISTS
        // =====================================================
        private static void UpsertUser(
            InvexaDbContext context,
            string email,
            string fullName,
            string phone,
            string role,
            string password,
            string defaultImage)
        {
            var user = context.Users.FirstOrDefault(u => u.UserEmail == email);

            if (user == null)
            {
                context.Users.Add(new User
                {
                    UserFullName = fullName,
                    UserEmail = email,
                    UserPhone = phone,
                    UserRole = role,
                    UserStatus = "Active",
                    UserPasswordHash = PasswordHasher.HashPassword(password),
                    UserProfileImageUrl = defaultImage,
                    UserCreatedAt = DateTime.Now
                });
            }
            else
            {
                user.UserFullName = fullName;
                user.UserPhone = phone;
                user.UserRole = role;
                user.UserStatus = "Active";

                if (string.IsNullOrWhiteSpace(user.UserProfileImageUrl))
                {
                    user.UserProfileImageUrl = defaultImage;
                }
            }
        }

        // =====================================================
        // MULTI-UOM – SEED BASE UNIT + DEFAULT CONVERSIONS
        // =====================================================
        private static void SeedItemUnitConversions(InvexaDbContext context)
        {
            // 🔒 Prevent duplicate seeding
            if (context.ItemUnitConversions.Any())
                return;

            var items = context.Items.ToList();

            foreach (var item in items)
            {
                var baseUnit = item.ItemUnitOfMeasure.Trim().ToLower();

                // BASE UNIT (MANDATORY)
                context.ItemUnitConversions.Add(new ItemUnitConversion
                {
                    ItemID = item.ItemID,
                    UnitName = baseUnit,
                    BaseUnitMultiplier = 1,
                    IsBaseUnit = true
                });

                // DEFAULT SECONDARY UNITS (editable later)
                context.ItemUnitConversions.Add(new ItemUnitConversion
                {
                    ItemID = item.ItemID,
                    UnitName = "pack",
                    BaseUnitMultiplier = 6,
                    IsBaseUnit = false
                });

                context.ItemUnitConversions.Add(new ItemUnitConversion
                {
                    ItemID = item.ItemID,
                    UnitName = "carton",
                    BaseUnitMultiplier = 24,
                    IsBaseUnit = false
                });
            }

            context.SaveChanges();
        }
    }
}
