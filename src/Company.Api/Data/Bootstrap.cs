using Company.Api.Auth;
using Microsoft.EntityFrameworkCore;

namespace Company.Api.Data;

public static class Bootstrap
{
    /// <summary>
    /// Creates the first administrator from configuration when no employee exists yet. Afterwards the administrator
    /// imports the employee list and links the accounts; the setting has no effect once employees exist.
    /// </summary>
    public static async Task RunAsync(CompanyDbContext db, IConfiguration configuration, ILogger logger)
    {
        if (await db.Employees.AnyAsync()) return;

        var accounts = configuration.GetSection("Bootstrap:AdminAccounts").Get<string[]>()?
            .Select(AccountNormalizer.Normalize).Where(a => a.Length > 0).Distinct().ToList() ?? new List<string>();
        if (accounts.Count == 0)
        {
            logger.LogWarning("No employees exist and Bootstrap:AdminAccounts is empty: nobody can sign in as administrator.");
            return;
        }

        var code = configuration["Bootstrap:AdminCode"] ?? "Z-000";
        var admin = new Employee { Code = code, Name = "Administrator", IsAdmin = true };
        foreach (var account in accounts) admin.Accounts.Add(new EmployeeAccount { EmployeeCode = code, Account = account });
        db.Employees.Add(admin);
        db.AuditEvents.Add(new AuditEvent
        {
            AtUtc = DateTime.UtcNow,
            Actor = "bootstrap",
            Action = "AdminCreated",
            EntityType = "Employee",
            EntityId = code,
            Details = string.Join(", ", accounts)
        });
        await db.SaveChangesAsync();
        logger.LogInformation("First administrator {Code} created for {Accounts}.", code, string.Join(", ", accounts));
    }
}
