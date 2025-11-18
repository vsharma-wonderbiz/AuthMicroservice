using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AuthMicroservice.Domain.Entities;

namespace AuthMicroservice.Infrastructure.Persistance.DbContexts
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        public DbSet<OAuthUser> OAuthUsers { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<UserOtp> UserOtp { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);

            // Ensure RefreshToken column length and add index for quick lookup
            modelBuilder.Entity<User>(b =>
            {
                b.Property(u => u.RefreshToken)
                 .HasMaxLength(255)
                 .IsUnicode(false); // hashed base64 is ASCII — storing as non-Unicode is fine

                b.HasIndex(u => u.RefreshToken)
                 .HasDatabaseName("IX_Users_RefreshToken");
            });

            // Optional: configure UserId PK type if needed, etc.
            base.OnModelCreating(modelBuilder);
        }



    }
}
