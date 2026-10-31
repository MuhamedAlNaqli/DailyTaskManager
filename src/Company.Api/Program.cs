using Company.Api.Auth;
using Company.Api.Data;
using Company.Api.Endpoints;
using Company.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Lets the same program run as a Windows service on the company server (a no-op when started from a console)
builder.Host.UseWindowsService();

// ---- Database: PostgreSQL in production; SQLite for development and tests ---------------------------------------------
var provider = builder.Configuration["Database:Provider"] ?? "postgres";
var connection = builder.Configuration.GetConnectionString("Default") ?? "Data Source=company-dev.db";
builder.Services.AddDbContext<CompanyDbContext>(options =>
{
    if (provider.Equals("sqlite", StringComparison.OrdinalIgnoreCase)) options.UseSqlite(connection);
    else options.UseNpgsql(connection);
});

// ---- Sign-in and authorisation ---------------------------------------------------------------------------------------
var mode = builder.AddCompanyAuthentication();
builder.Services.AddCompanyAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ChartService>();
builder.Services.AddScoped<TimesheetService>();

var app = builder.Build();

// A new installation has nobody who could import the employees: the accounts named in Bootstrap:AdminAccounts become the
// first administrator, once, while the employee table is empty
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
    await db.Database.EnsureCreatedAsync();
    await Bootstrap.RunAsync(db, app.Configuration, app.Logger);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapMeEndpoints(mode);
app.MapAdminEndpoints();
app.MapTimesheetEndpoints();

app.Run();

/// <summary>Visible to the tests (WebApplicationFactory)</summary>
public partial class Program
{
}
