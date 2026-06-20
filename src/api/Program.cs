using System.Threading.RateLimiting;
using Azure.Communication.Email;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using FluentValidation;
using LogisticsApi.BackgroundJobs;
using LogisticsApi.Data;
using LogisticsApi.Middleware;
using LogisticsApi.Services;
using LogisticsApi.Services.AssignmentEngine;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration["Sql:ConnectionString"]
            ?? builder.Configuration.GetConnectionString("Sql")
            ?? throw new InvalidOperationException("SQL connection string not configured"),
        sql => sql.EnableRetryOnFailure(3)));

// ── Authentication — Azure Entra ID ───────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("EntraId"));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Driver",      p => p.RequireRole("Driver"))
    .AddPolicy("Coordinator", p => p.RequireRole("Coordinator", "Manager", "Admin"))
    .AddPolicy("Manager",     p => p.RequireRole("Manager", "Admin"))
    .AddPolicy("Mechanic",    p => p.RequireRole("Mechanic", "Manager", "Admin"))
    .AddPolicy("Admin",       p => p.RequireRole("Admin"));

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? ["http://localhost:3000", "http://localhost:5173"];
    opts.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader());
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("api", o =>
    {
        o.PermitLimit = 60;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 10;
    });
    opts.RejectionStatusCode = 429;
});

// ── Azure Storage ─────────────────────────────────────────────────────────────
var storageConn = builder.Configuration["Storage:ConnectionString"];
if (!string.IsNullOrEmpty(storageConn))
{
    builder.Services.AddSingleton(new BlobServiceClient(storageConn));
    builder.Services.AddSingleton(new QueueServiceClient(storageConn));
}

// ── Azure Communication Services ──────────────────────────────────────────────
var acsConn = builder.Configuration["Acs:ConnectionString"];
if (!string.IsNullOrEmpty(acsConn))
    builder.Services.AddSingleton(new EmailClient(acsConn));

// ── In-Memory Cache (SMB plan — no Redis) ────────────────────────────────────
builder.Services.AddMemoryCache();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IAssignmentEngine, AssignmentEngineService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// ── Background Jobs ───────────────────────────────────────────────────────────
builder.Services.AddHostedService<MaintenanceReminderJob>();

// ── Validation ────────────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Logistics & Fleet Management API",
        Version = "v1",
        Description = "REST API for the Logistics Platform"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// ── Auto-migrate on startup (all environments) ───────────────────────────────
// EF migrations are idempotent — safe to run on every startup.
// Seeding is gated by Demo:SeedOnStartup (true in Docker, false in Production).
{
    using var scope = app.Services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var cfg    = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    db.Database.Migrate();

    if (cfg.GetValue<bool>("Demo:SeedOnStartup"))
        await LogisticsApi.Data.DatabaseSeeder.SeedAsync(db, cfg, logger);
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();

app.MapControllers().RequireRateLimiting("api");
app.MapHealthChecks("/health");

app.Run();
