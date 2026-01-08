using AuthMicroservice.Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using AuthMicroservice.Infrastructure.Persistance.DbContexts;
using AuthMicroservice.Application.Mapping;
using Microsoft.EntityFrameworkCore.Diagnostics;
using AuthMicroservice.Infrastructure.Persistance.seeding;

using Serilog;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Diagnostics;
using FluentValidation;
using AuthMicroservice.Application.Validators;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "auth-service")
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://seq:5341")
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddHttpClient("Ocelot")
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator });


// ---------- Services ----------
builder.Services.AddControllers().AddFluentValidation(fv => fv.DisableDataAnnotationsValidation = true); ;
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCustomServices();
builder.Services.AddCustomAuthentication(builder.Configuration);

//validators registrion 
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// DbContext
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultStr"),
        sqlOptions =>
        {
            // ensure migrations go into the infrastructure assembly
            sqlOptions.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName);
        })
    .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuting))
);

// CORS
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



// ---------- OpenTelemetry ----------
var jaegerHost = builder.Configuration["Jaeger:Host"] ?? "localhost";
var jaegerPort = int.TryParse(builder.Configuration["Jaeger:Port"], out var p) ? p : 6831;


builder.Services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("auth-service")
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault().AddService("auth-service"))
            .AddJaegerExporter();
    });

// ---------- Build app ----------
var app = builder.Build();

// ---------- Optional: apply migrations on startup (dev convenience) ----------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<UserDbContext>();

        // apply migrations (remove or guard in production)
        try
        {
            context.Database.Migrate();
        }
        catch (Exception migEx)
        {
            var mLogger = services.GetRequiredService<ILogger<Program>>();
            mLogger.LogWarning(migEx, "Database migrate failed at startup (continuing).");
        }

        // seeding
        try
        {
            await DefaultAsset.SeedAsync(context);
        }
        catch (Exception seedEx)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(seedEx, "❌ An error occurred while seeding the database.");
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to prepare DB/seed at startup.");
    }
}

// ---------- Middleware pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serilog request logging
app.UseSerilogRequestLogging();

// Add a request-id + put TraceId into log context
app.Use(async (ctx, next) =>
{
    // propagate X-Request-ID or create one
    var requestId = ctx.Request.Headers.ContainsKey("X-Request-ID")
        ? ctx.Request.Headers["X-Request-ID"].ToString()
        : Guid.NewGuid().ToString("N");

    ctx.Response.Headers["X-Request-ID"] = requestId;

    // prefer Activity.Current.TraceId when available (OTel)
    var traceId = Activity.Current?.TraceId.ToString() ?? requestId;

    using (Serilog.Context.LogContext.PushProperty("RequestId", requestId))
    using (Serilog.Context.LogContext.PushProperty("TraceId", traceId))
    {
        await next();
    }
});

// Important order: CORS → Authentication → Authorization
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();



app.Run();
