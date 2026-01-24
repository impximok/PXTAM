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
            // ADMIN – System
            // ===============================
            UpsertUser(
                context,
                email: "emilycarter@invexa.com",
                fullName: "Emily Carter",
                phone: "0123456789",
                role: "Admin",
                password: "Emily@123",
                defaultImage: "/images/users/Admin.jpg"
            );

            // ===============================
            // ADMIN – You
            // ===============================
            UpsertUser(
                context,
                email: "impximok@gmail.com",
                fullName: "impximok",
                phone: "0123456791",
                role: "Admin",
                password: "Impximok@123",
                defaultImage: "/images/users/Admin.jpg"
            );

            // ===============================
            // MANAGER
            // ===============================
            UpsertUser(
                context,
                email: "sophiawilliams@invexa.com",
                fullName: "Sophia Williams",
                phone: "0123456790",
                role: "Manager",
                password: "Sophia@123",
                defaultImage: "/images/users/Manager.jpg"
            );

            // ===============================
            // STAFF
            // ===============================
            UpsertUser(
                context,
                email: "danielthompson@invexa.com",
                fullName: "Daniel Thompson",
                phone: "0123456791",
                role: "Staff",
                password: "Daniel@123",
                defaultImage: "/images/users/Staff.jpg"
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
                // ===============================
                // INSERT (FIRST TIME ONLY)
                // ===============================
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
                // ===============================
                // UPDATE (SAFE / NON-DESTRUCTIVE)
                // ===============================
                user.UserFullName = fullName;
                user.UserPhone = phone;
                user.UserRole = role;
                user.UserStatus = "Active";

                // ⚠️ CRITICAL RULE:
                // Only assign default image if user never set one
                if (string.IsNullOrWhiteSpace(user.UserProfileImageUrl))
                {
                    user.UserProfileImageUrl = defaultImage;
                }

                // ❌ DO NOT reset password
                // ❌ DO NOT override user profile image
            }
        }
    }
}
