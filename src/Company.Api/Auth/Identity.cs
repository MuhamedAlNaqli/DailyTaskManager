using System.Security.Claims;
using Company.Api.Contracts;
using Company.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.EntityFrameworkCore;

namespace Company.Api.Auth;

public static class AccountNormalizer
{
    /// <summary>Sign-in names are compared without regard to case and surrounding spaces</summary>
    public static string Normalize(string? account) => (account ?? "").Trim().ToLowerInvariant();
}

public static class Roles
{
    public const string Admin = "Admin";

    /// <summary>Someone who has people reporting to them, or is a designated approver</summary>
    public const string Approver = "Approver";

    public const string FinalApprover = "FinalApprover";
}

public static class Identities
{
    public const string EmployeeCodeClaim = "emp_code";
    public const string ResolvedClaim = "emp_resolved";

    public const string RegisteredPolicy = "Registered";
    public const string AdminPolicy = "Admin";

    /// <summary>
    /// The names a caller is known by: the Windows name (DOMAIN\user), the user principal name, an Entra name or the
    /// e-mail address. Any of them may be what the administrator linked to the employee.
    /// </summary>
    public static IReadOnlyList<string> Candidates(ClaimsPrincipal user)
    {
        var names = new List<string?> { user.Identity?.Name };
        foreach (var type in new[] { ClaimTypes.Upn, "upn", "preferred_username", "unique_name", ClaimTypes.Email, "email" })
            names.AddRange(user.FindAll(type).Select(c => c.Value));

        return names.Select(AccountNormalizer.Normalize).Where(n => n.Length > 0).Distinct().ToList();
    }

    public static string? EmployeeCode(ClaimsPrincipal user) => user.FindFirstValue(EmployeeCodeClaim);

    /// <summary>The audit actor: the employee code when known, otherwise the sign-in name</summary>
    public static string Actor(ClaimsPrincipal user) =>
        EmployeeCode(user) ?? user.Identity?.Name ?? "unknown";
}

/// <summary>After sign-in, finds the employee the account is linked to and adds the employee code and the roles to the caller</summary>
public sealed class EmployeeClaimsTransformation : IClaimsTransformation
{
    private readonly CompanyDbContext _db;

    public EmployeeClaimsTransformation(CompanyDbContext db) => _db = db;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || principal.HasClaim(c => c.Type == Identities.ResolvedClaim)) return principal;

        var candidates = Identities.Candidates(principal);
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(Identities.ResolvedClaim, "1"));

        var account = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => candidates.Contains(a.Account));
        if (account != null)
        {
            var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Code == account.EmployeeCode && e.IsActive);
            if (employee != null)
            {
                identity.AddClaim(new Claim(Identities.EmployeeCodeClaim, employee.Code));
                if (employee.IsAdmin) identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Admin));
                if (employee.IsFinalApprover) identity.AddClaim(new Claim(ClaimTypes.Role, Roles.FinalApprover));
                if (await _db.Employees.AnyAsync(e => e.SupervisorCode == employee.Code || e.DesignatedApproverCode == employee.Code))
                    identity.AddClaim(new Claim(ClaimTypes.Role, Roles.Approver));
            }
        }

        principal.AddIdentity(identity);
        return principal;
    }
}

public static class AuthorizationSetup
{
    public static void AddCompanyAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IClaimsTransformation, EmployeeClaimsTransformation>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy(Identities.RegisteredPolicy, p => p.RequireAuthenticatedUser().RequireClaim(Identities.EmployeeCodeClaim));
            options.AddPolicy(Identities.AdminPolicy, p => p.RequireAuthenticatedUser().RequireClaim(Identities.EmployeeCodeClaim).RequireRole(Roles.Admin));
        });
    }
}

/// <summary>Answers a refusal with a body the desktop app can show: signed in but not registered, or not allowed</summary>
public sealed class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden && context.User.Identity?.IsAuthenticated == true)
        {
            var registered = Identities.EmployeeCode(context.User) != null;
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(registered
                ? new ApiErrorDto("forbidden", "You are not allowed to do this.")
                : new ApiErrorDto("not-registered", "Your account is not linked to an employee. Ask the administrator."));
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
