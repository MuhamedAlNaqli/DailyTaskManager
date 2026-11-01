using Company.Api.Auth;
using Company.Api.Contracts;
using Company.Api.Data;
using Company.Api.Endpoints;
using Company.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Lets the same program run as a Windows service on the company server (a no-op when started from a console)
builder.Host.UseWindowsService();

// ---- Database: PostgreSQL in production; SQLite for development and tests ---------------------------------------------
var provider = builder.Configuration["Database:Provider"] ?? "postgres";
var connection = builder.Configuration.GetConnectionString("Default") ?? "Data Source=company-dev.db";
builder.Services.AddDbContext<CompanyDbContext>(options =>
{
    // A sheet has four collections; read together in one query they multiply, so each is read on its own
    if (provider.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(connection, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    else
        options.UseNpgsql(connection, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
});

// A request is a few kilobytes; the biggest is the CSV of the employee list
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 2 * 1024 * 1024);

// ---- Sign-in and authorisation ---------------------------------------------------------------------------------------
var mode = builder.AddCompanyAuthentication();
builder.Services.AddCompanyAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ChartService>();
builder.Services.AddScoped<EmployeeAdminService>();
builder.Services.AddScoped<SheetDtoBuilder>();
builder.Services.AddScoped<TimesheetService>();
builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<ReminderService>();
builder.Services.AddScoped<PrintService>();
builder.Services.TryAddSingleton(TimeProvider.System);
builder.Services.AddSingleton<EmployeeLookupCache>();

var app = builder.Build();

// A new installation has nobody who could import the employees: the accounts named in Bootstrap:AdminAccounts become the
// first administrator, once, while the employee table is empty
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
    // PostgreSQL (production) is created and updated by the migrations; the SQLite file of development and tests is simply created.
    // A company that wants the service account to have no right to change the schema runs "Company.Api.dll --migrate" once per
    // version with another account and sets Database:MigrateOnStart to false.
    var migrateOnly = args.Contains("--migrate");
    if (db.Database.IsNpgsql())
    {
        if (migrateOnly || app.Configuration.GetValue("Database:MigrateOnStart", true)) await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    await Bootstrap.RunAsync(db, app.Configuration, app.Logger);
    if (migrateOnly) return;
}

// What goes wrong unexpectedly is logged and answered in the same form as every other refusal, not as a page
app.UseExceptionHandler(errors => errors.Run(async context =>
{
    var failure = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(failure, "Unhandled error in {Method} {Path}", context.Request.Method, context.Request.Path);
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new ApiErrorDto("server-error", "The server could not complete the request. Try again; if it repeats, tell the administrator."));
}));

// Anything that changes data must come from the desktop app: it carries a header a web page of another site cannot add,
// and a request that names another origin is refused
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var changes = !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method));
    if (changes)
    {
        if (context.Request.Headers[ApiHeaders.RequestedWith] != ApiHeaders.RequestedWithValue)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiErrorDto("missing-header", $"Requests that change data need the header {ApiHeaders.RequestedWith}."));
            return;
        }

        var origin = context.Request.Headers.Origin.ToString();
        if (origin.Length > 0 && (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || !string.Equals(uri.Authority, context.Request.Host.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiErrorDto("forbidden-origin", "The request comes from another site."));
            return;
        }
    }

    await next();
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var url in app.Urls)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var parsed) && parsed.Scheme == Uri.UriSchemeHttp && !parsed.IsLoopback && parsed.Host != "localhost")
            app.Logger.LogWarning("The server listens on {Url} without HTTPS: Windows sign-in and the timesheets travel in clear text. Use HTTPS outside a test.", url);
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapMeEndpoints(mode);
app.MapAdminEndpoints();
app.MapTimesheetEndpoints();
app.MapApprovalEndpoints();
app.MapExportEndpoints();

app.Run();

/// <summary>Visible to the tests (WebApplicationFactory)</summary>
public partial class Program
{
}
