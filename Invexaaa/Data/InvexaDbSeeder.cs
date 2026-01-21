using Invexaaa.Models.Invexa;
using Invexaaa.Helpers;
namespace Invexaaa.Data
{
    public static class InvexaDbSeeder
    {
        public static void Seed(InvexaDbContext context)
        {
            if (!context.Users.Any())
            {
                context.Users.AddRange(

                    // Admin
                    new User
                    {
                        UserFullName = "Emily Carter",
                        UserEmail = "emilycarter@invexa.com",
                        UserPasswordHash = PasswordHasher.HashPassword("Emily@123"),     // hash later
                        UserPhone = "0123456789",
                        UserRole = "Admin",
                        UserStatus = "Active",
                        UserProfileImageUrl = "/images/users/Admin",
                        UserCreatedAt = DateTime.Now
                    },

                    // Manager
                    new User
                    {
                        UserFullName = "Sophia Williams",
                        UserEmail = "sophiawilliams@invexa.com",
                        UserPasswordHash = PasswordHasher.HashPassword("Sophia@123"),
                        UserPhone = "0123456790",
                        UserRole = "Manager",
                        UserStatus = "Active",
                        UserProfileImageUrl = "/images/users/Manager",
                        UserCreatedAt = DateTime.Now
                    },

                    // Staff
                    new User
                    {
                        UserFullName = "Daniel Thompson",
                        UserEmail = "danielthompson@invexa.com",
                        UserPasswordHash = PasswordHasher.HashPassword("Daniel@123"),
                        UserPhone = "0123456791",
                        UserRole = "Staff",
                        UserStatus = "Active",
                        UserProfileImageUrl = "/images/users/Staff",
                        UserCreatedAt = DateTime.Now
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
