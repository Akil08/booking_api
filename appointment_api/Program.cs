using System.Text;
using appointment_api.Data;
using appointment_api.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Hangfire Configuration
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
builder.Services.AddHangfireServer();

// Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<DailyJobService>();

// Register Hangfire Job Registrar (fixes the stack overflow issue)



builder.Services.AddHostedService<HangfireJobRegistrar>();

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? string.Empty;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    
    var daily = scope.ServiceProvider.GetRequiredService<DailyJobService>();
    daily.EnsureTodayAsync().GetAwaiter().GetResult();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseHangfireDashboard(app.Configuration["Hangfire:DashboardPath"] ?? "/hangfire");

// ✅ NO MORE RecurringJob.AddOrUpdate here! (Moved to HangfireJobRegistrar)

app.Run();