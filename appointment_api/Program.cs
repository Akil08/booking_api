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

// how to use hangfire ?
// Hangfire is a library for scheduling and executing background jobs in .NET applications.
// To use Hangfire, you need to configure it in your application, 
// set up a storage mechanism (like a database), 
// and then define the jobs you want to run in the background.
// i mean here in this code how do i use it ?
// In this code, Hangfire is configured to use PostgreSQL as its storage mechanism.
// The line `builder.Services.AddHangfire(config => config.UsePostgreSqlStorage(...))
// sets up Hangfire to store its data in a PostgreSQL database specified by the connection string "HangfireConnection".
// The line `builder.Services.AddHangfireServer()` adds the Hangfire server to the application, which will process the background jobs.
// Later in the code, a recurring job is defined 
// using `RecurringJob.AddOrUpdate<DailyJobService>("daily-reset", service => service.RunDailyResetAsync(), "0 3 * * *")`,
// which schedules the `RunDailyResetAsync` method of the `DailyJobService` to run every day at 3 AM.

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<DailyJobService>();

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

RecurringJob.AddOrUpdate<DailyJobService>("daily-reset", service => service.RunDailyResetAsync(), "0 3 * * *");

app.Run();
