using AuthMicroservice.Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using AuthMicroservice.Infrastructure.Persistance.DbContexts;
using AuthMicroservice.Application.Mapping;
using Microsoft.EntityFrameworkCore.Diagnostics;
using AuthMicroservice.Infrastructure.Persistance.seeding;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




builder.Services.AddCustomServices();
builder.Services.AddCustomAuthentication(builder.Configuration);
//builder.Services.AddHostedService<AverageProcessorService>();


builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultStr"),
        sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName); 
        }
    )
    .ConfigureWarnings(warnings =>
        warnings.Ignore(RelationalEventId.CommandExecuting)) 
);



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:3000",
            "https://localhost:5173",
            "https://localhost:3000"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}




using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<UserDbContext>();

    try
    {
        await DefaultAsset.SeedAsync(context);
        //var dummySeeder = new DummySeed();               
        //await dummySeeder.SeedDummyDataAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while seeding the database.");
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();


app.UseCors("AllowFrontend");


app.MapControllers();

app.Run();
