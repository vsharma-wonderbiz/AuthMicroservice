using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthMicroservice.Infrastructure.Persistance.DbContexts;
using AuthMicroservice.Domain.Entities;
using AuthMicroservice.Infrastructure.Persistance.seeding;

namespace AuthMicroservice.Infrastructure.Persistance.seeding
{
    public class DefaultAsset
    {

        public static async Task SeedAsync(UserDbContext context)
        {


            context.Database.EnsureCreated(); // Create DB if not exists

            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                Console.WriteLine("No admin found. Creating default admin...");

                var admin = new User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    Role = "Admin",
                    PasswordHash = HashPassword("Admin@123") // Use same hash as normal users
                };

                context.Users.Add(admin);
                context.SaveChanges();

                Console.WriteLine("✅ Default admin created!");
            }
            else
            {
                Console.WriteLine("⚡ Admin already exists. Skipping seed.");
            }
        }


        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
