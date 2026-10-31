# Company API: guide for the company's IT

This guide is for whoever runs the server of the employee timesheet API (`src/Company.Api`). It covers what is needed,
how to configure sign-in, and how to test it before any employee uses it. Status: the sign-in, employee administration,
holidays and audit are implemented; month entry, submission, approval, reminders and the printed sheet work.

## 1. What is needed

| Item | Notes |
| --- | --- |
| A Windows server in the company network | Domain-joined for Kerberos. A Windows PC is enough for a pilot. |
| .NET 8 runtime (ASP.NET Core) | Or publish the API self-contained. |
| PostgreSQL 14 or newer | One database, for example `CompanyTimesheet`, reachable only from the API. Daily backups with a tested restore. |
| An HTTPS certificate from the company's certificate authority | Kerberos tickets and timesheet data must not travel in clear text. |
| A service account | Runs the API as a Windows service and, for Kerberos, owns the service principal name (section 3). |

## 2. Settings (`appsettings.json` or environment variables)

| Setting | Meaning |
| --- | --- |
| `ConnectionStrings:Default` | PostgreSQL connection string |
| `Database:Provider` | `postgres` (default) or `sqlite` (development only) |
| `Authentication:Mode` | `Windows` (on-premises Active Directory, the default), `Entra`, `Both`, or `Test` (development only, refused elsewhere) |
| `Authentication:Entra:TenantId`, `ClientId`, `Audience` | Needed for `Entra` and `Both`; the server refuses to start without them |
| `Bootstrap:AdminAccounts` | Sign-in names of the first administrators, for example `["CORP\\jane.admin"]`. Used once, while no employee exists. |
| `Bootstrap:AdminCode` | Employee code given to that first administrator (default `Z-000`) |
| `Reminders:StartFrom` | The first month that is reminded about, as `yyyy-MM` (for example the go-live month). Earlier months are ignored. Without it, last month counts as overdue on the first day. |
| `Kestrel:Endpoints:Https:Url` / certificate | The HTTPS address and certificate of the server |

Environment variables use double underscores, for example `Authentication__Mode=Windows`.

## 3. Windows (Kerberos) sign-in

1. Run the API as a Windows service under the service account (`UseWindowsService` is built in).
2. Register the service principal name so that clients get Kerberos instead of the older NTLM fallback:
   `setspn -S HTTP/timesheet.company.local CORP\svc-timesheet` (use the real host name and account).
3. Clients call the API by that host name over HTTPS. The desktop app sends the user's Windows identity automatically.
4. Users in the domain need no password prompt. A caller without a valid ticket receives `401` with `WWW-Authenticate: Negotiate`.

## 4. Testing the sign-in before go-live

From a domain PC with `curl` (Windows 10 or newer has it), signed in as a normal user:

```powershell
curl.exe --negotiate -u : https://timesheet.company.local/api/auth/diagnostics
```

The answer shows what the server sees: `identityName` (for example `CORP\amra.adzamija`), `authenticationType`
(`Kerberos` is what we want; `NTLM` works but means the service principal name is missing), the names the account is
known by (`candidates`) and the `employeeCode` it is linked to (empty until the administrator links it). Please send us
this output for two or three test users.

To see which Active Directory attributes the company fills in, run this for one or two users (no passwords involved):

```powershell
Get-ADUser -Identity <signinname> -Properties displayName, mail, manager, employeeID, employeeNumber, department, title |
  Select-Object SamAccountName, UserPrincipalName, DisplayName, Mail, Manager, EmployeeID, EmployeeNumber, Department, Title
```

## 5. Microsoft Entra ID (optional, "just in case")

Only if the company wants it, now or later:

1. In the Entra admin centre, register an application for the API. Note the tenant id and the application (client) id.
2. Expose the API (an application id URI), and register the desktop app as a client that may request it.
3. Set `Authentication:Mode` to `Entra` (Entra only) or `Both` (Windows and Entra) and fill `Authentication:Entra`.
4. The desktop app then signs in with the Windows account broker, which also honours Conditional Access (for example MFA).

With `Both`, a request with a bearer token is validated as Entra and every other request goes through Windows.

The database is created and updated by the API itself: at start-up it applies the migrations that are missing, so the
service account needs the right to create tables in that database (and nothing else). Back up before installing a new version.

## 6. First start

1. Start the API with `Bootstrap:AdminAccounts` set. The first administrator is created and is recorded in the audit log.
2. The administrator signs in (the desktop app or `curl` as above) and imports the employee list (a preview shows
   errors and warnings first), then links accounts: by an attribute if the company has one, by a name-matching
   preview, or from a small CSV.
3. The administrator sets "reports to" for everyone, flags the lawyer as final approver, flags the people of finance (they receive the approved timesheets of everybody as an Excel or PDF sheet), and gives the person at the top of
   the chart a designated approver. The server refuses a chart where someone would be their own supervisor.
4. The administrator enters the state holidays of the year; religious holidays are marked per employee.

## 7. What the server records

Every change to master data (employees, accounts, chart, holidays) is written to an append-only audit log in the same
save: who, when, what. The application cannot change or delete audit rows. The log is available to administrators at
`/api/admin/audit`.

## 7b. The desktop app

On each employee PC the app needs one setting, in `appsettings.local.json` next to the program (or the environment
variable `Company__ApiUrl`):

```json
{ "Company": { "ApiUrl": "https://timesheet.company.local" } }
```

The employee chooses "Employee sign-in (company account)" on the first screen; the choice is remembered. In employee mode the app also registers a Windows scheduled task for the signed-in user (no administrator rights, no password) that asks the server every 30 minutes what is due and shows a notification. Plain `http://`
is refused except for the local computer.

## 8. Not yet implemented

Everything of the timesheet works (entries, validation, submission, approval, reminders, printed sheet, database
migrations). Not built: deputies for approvers, an administrator screen in the app (use the API), a screen for stored
revisions, email reminders; see `docs/EMPLOYEE_TIMESHEET_FEASIBILITY.md` section 17.
