using System.Linq;
using AuthMicroservice.Infrastructure.Persistance.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace AuthMicroservice.IntegrationTests.CustomWebApplication
{
    // CustomWebApplicationFactory ka kaam hai
    // real ASP.NET Core app ko test ke liye in-memory start karna
    // without touching real database
    public class CustomWebApplicationFactory
        : WebApplicationFactory<Program> // Program.cs se real app start hoti hai
    {
        // SQLite in-memory DB ka connection
        // isko field banaya kyunki connection open rehna chahiye
        private SqliteConnection _connection = default!;

        // ye method app start hone se pehle call hota hai
        // yahi pe hum DI services ko modify kar sakte hai
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // ConfigureServices hume DI container deta hai
            // jisme saare services, DbContext, repositories registered hote hai
            builder.ConfigureServices(services =>
            {
                // yaha hum real database configuration dhundh rahe hai
                // agar hum isko remove nahi karenge toh
                // app real DB se connect karne ki try karegi (which we don't want)
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<UserDbContext>));

                // agar real DbContext mila
                if (descriptor != null)
                {
                    // toh usko remove kar do
                    services.Remove(descriptor);
                }

                // yaha hum SQLite ka in-memory database bana rahe hai
                // DataSource=:memory: matlab DB RAM me rahega
                _connection = new SqliteConnection("DataSource=:memory:");

                // connection open rakhna bahut important hai
                // connection close hua toh DB delete ho jayega
                _connection.Open();

                // yaha hum UserDbContext ko SQLite in-memory ke sath register kar rahe hai
                services.AddDbContext<UserDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });

                // yaha hum manually service provider bana rahe hai
                // taki database create ho sake
                var sp = services.BuildServiceProvider();

                // ek scope create kar rahe hai
                using var scope = sp.CreateScope();

                // UserDbContext nikal rahe hai
                var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();

                // database + tables create kar deta hai
                // bina migration ke
                db.Database.EnsureCreated();
            });
        }

        // jab test host dispose hota hai
        // tab SQLite connection bhi close karna chahiye
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // in-memory DB cleanup
            _connection?.Dispose();
        }
    }
}
