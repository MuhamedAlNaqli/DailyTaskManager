# Employee timesheets (šihtarica): feasibility and proposed design

Status: **assessment for review; step 1 (the domain library) is implemented**, see section 12. Nothing else is implemented.

Source: the analysis in `E:\Internal Projects\Timesheet` (`docs/timesheet-analysis`, 37 questions and answers,
the sample sheet, the filling instruction, the employee list). The architecture file (S5) there is empty, and the
original Excel workbook was not supplied (the stakeholders say it has no formulas, only display).

Decisions taken so far:

- The **consultant side stays a standalone app** with everything it has today and its own local database, so a
  consultant's data is theirs alone. It never talks to the company API.
- The **corporate data is stored in one place for all employees**, behind a **separate API** that holds the business
  logic. The desktop app is the front end for it, with an **Employee mode** that is separate from the consultant mode.
- Users sign in with **Windows authentication against the on-premises Active Directory**. **Microsoft Entra ID** is
  added as a second, switchable option in case it is needed.
- The system runs **inside the company network**.
- The desktop app must **remind** users to fill in and submit their timesheets.

## 1. Verdict

**It can be built in this solution, and the chosen architecture is the right one. It is also the simpler of the options
discussed: the consultant app does not change.** A desktop front end is the better
fit than a web application for one requirement in particular: reminders. A browser page cannot nudge anyone while it is
closed, but the desktop app already has the machinery (a scheduled Windows task that runs even when the app is closed,
and tray notifications). The API keeps rules, workflow, audit and authorisation in one place, which a desktop client
that talks to the database directly could never guarantee for 112 employees.

## 2. Architecture

```
  Desktop app (Avalonia)                                  company network (HTTPS, internal certificate)
   ├ Consultant mode ── local database (PostgreSQL on the user's PC), exactly as today, no network
   └ Employee mode  ────────────────────────────────────────────────► API (ASP.NET Core, Windows server)
        Windows sign-in (Kerberos) or Entra token                        Timesheet logic ─► corporate database
  Scheduled reminder task (runs as the user, no password)                Identity: AD / Entra, roles, audit
```

| Part | Content |
| --- | --- |
| **API** | One ASP.NET Core application for the corporate data: employees, calendar and holidays, time codes, month entries, overtime split and classification, validation, workflow, revisions, audit, reminders, identity. |
| **Domain library** | Pure code without I/O, shared by the API and the tests: calendar, overtime windows, monthly fund, validation rules, workflow state machine. Same style as `MissingDays` and `ReminderEvaluator` today. |
| **Desktop app** | Avalonia, as today. At the sign-in screen the user picks the mode. **Consultant mode** is the current app, unchanged. **Employee mode** signs in with the Windows account and works only against the API. The two modes share the design system and the update mechanism, never data or sign-in. |
| **Databases** | The consultant database stays on the consultant's PC. The corporate database is one central PostgreSQL database that only the API reaches. |

Because the consultant mode needs no server, a consultant who is not an employee (or who works outside the company
network) can use the app exactly as before. The employee mode needs the company network.

Splitting the two modes into two executables later is possible (they only share libraries), but one app with two modes
means one installer, one updater and one place for the design system, so I would start with one.

## 3. Windows authentication

**On-premises Active Directory (primary):**

1. The API runs on a domain-joined **Windows server** and uses `Negotiate` authentication (Kerberos, NTLM as
   fallback). The API can run on Kestrel or behind IIS.
2. The desktop app calls the API with the current Windows identity (`UseDefaultCredentials`). There is no password
   prompt, no stored secret, and no "remember me" for employees.
3. The API reads the account (for example `DOMAIN\amra.adzamija`) and maps it to an **employee** (code `Z-NNN`).
   An account that maps to nobody gets a clear "not registered" answer.
4. Roles (service head, legal reviewer, administrator) and the responsible-person relationships come from the
   application's own tables. AD groups can additionally mark who is an administrator or the lawyer.
5. The reminder task runs as the signed-in user, so it can ask the API silently as well.

**Microsoft Entra ID (second option, same code base):**

- The API accepts a bearer token from Entra (`Microsoft.Identity.Web`). The desktop app gets it with MSAL and the
  Windows account broker, which also signs in silently on a joined PC and honours Conditional Access (MFA).
- A configuration setting chooses `Windows`, `Entra` or `Both`. With `Both`, a request with a bearer token is
  validated as Entra, and any other request goes through Negotiate. Everything after authentication (mapping to an
  employee, roles) is identical.
- Entra needs an app registration (client id, tenant id) from the company's administrator. It stays off until needed.

**Safeguards that come with it:** HTTPS with the company's internal certificate (Kerberos tickets and data must not
travel in clear), a switchable **test sign-in** for development only (never enabled in production), and auditing of
who signed in and what they did.

**Testing:** this development PC is in a workgroup, so real Windows authentication cannot be tested here. A test
domain, a company test server or a small lab VM with a domain controller is needed early. The Entra path can be tried
with a work account on this PC after an app registration exists.

### 3b. Linking accounts to employees and the test environment

**Which AD attribute?** Nobody knows yet, so the design does not depend on one. Active Directory normally has these
fields on every user, any of which can be used:

| Attribute | Use |
| --- | --- |
| `sAMAccountName`, `userPrincipalName`, `mail` | The sign-in name. Always present. |
| `displayName` | Usually "first last", comparable to the employee list's name |
| `manager` | The user's supervisor, a ready-made source for the approval chain **if HR keeps it filled** |
| `employeeID`, `employeeNumber` | Could hold the code `Z-NNN`, if the company fills it |
| `department`, `title` | Optional context |

How the link is made, in order of preference:

1. If `employeeID` or `employeeNumber` holds the code, map automatically.
2. Otherwise **match by name** (ignoring case and diacritics) with a preview, and let the administrator fix the
   leftovers on screen. 112 people take minutes, not days.
3. Always possible: the administrator imports a small CSV (code, sign-in name).

To find out what the company has, someone with domain access can run this on any domain PC with the AD tools, for one
or two sample users, and send the output (no passwords are involved):

```powershell
Get-ADUser -Identity <signinname> -Properties displayName, mail, manager, employeeID, employeeNumber, department, title |
  Select-Object SamAccountName, UserPrincipalName, DisplayName, Mail, Manager, EmployeeID, EmployeeNumber, Department, Title
```

**Server and test environment:** the API needs a domain-joined Windows server (any supported Windows Server, or a
Windows PC for a pilot) with HTTPS from the company's internal certificate authority. For testing, either the company
provides a test server and two test accounts, or a lab is built on a PC with Hyper-V: a Windows Server evaluation VM
as domain controller plus a joined client VM. I can write the lab setup script if the company cannot provide one.

## 4. Reminders

| Reminder | Who | When | How |
| --- | --- | --- | --- |
| Fill in and submit the timesheet | Employee | Before the 25th (moved to the first working day when the 25th is a weekend), and when it is overdue | Tray notification from the scheduled task, plus a banner in the app |
| Returned with a message | Employee | When the service head or lawyer returns it | Notification and an item in the work queue |
| Items waiting for approval | Service head, lawyer | When something is submitted, and daily while overdue; all approvals before the next month starts | Notification and work queue |
| Missing hours (existing consultant reminder) | Consultant mode | As today | Unchanged, answered by the local database |

How it works: the **API decides** what is due (one rule set for the banner, the task and any email), and the desktop
task only asks "what is due for me?" and shows it. The existing `ReminderRunner`, `TrayNotifier` and the scheduled
task are reused; in Employee mode the source of truth is the API, in Consultant mode it stays the local database. If the API
cannot be reached (laptop outside the network), the task shows nothing rather than a wrong reminder, and it keeps the
last known deadline so a due date is still honoured. Email reminders from the API are an optional addition for people
who rarely open the app.

## 5. Separate employee and consultant views

| Level | Consultant mode | Employee mode |
| --- | --- | --- |
| Screens | Daily tasks, calendar, reports, clients, documents (as today) | My month, absences, overtime, my submissions; approvals for service heads; legal review for the lawyer |
| Sign-in | Local account, Google, Microsoft (as today) | Windows account only |
| Data | Local database, private to the consultant | Corporate database, behind the API |
| Network | None needed | Company network |

The mode is chosen on the sign-in screen and remembered. Nothing the consultant stores is ever sent to the API.

## 6. Approval chain (decided)

Every person has **one supervisor** ("reports to"). A timesheet climbs that chain: the employee's team lead approves
first, then the team lead's supervisor, and so on. The length of the chain is whatever the organisation chart is; the
application does not fix it at two or three levels. Example from the stakeholders: employee, team lead, lawyer,
finance.

Rules (the defaults below fill what was not said; each can be changed without redesign):

| Rule | Default |
| --- | --- |
| Data | `Employee.ReportsTo` (the supervisor, maintained by the administrator, or read from the AD `manager` attribute if IT keeps it filled). Cycles are rejected. |
| Steps for one timesheet | The supervisors above the employee, in order, up to and including the first one marked **final approver** (the lawyer). If nobody is marked, up to the top of the chain. |
| Whose own timesheet needs whom | Everyone's goes up *their own* chain. The lawyer's chain is finance, so the lawyer's timesheet is approved by finance. |
| A person at the top with no supervisor | Needs a **designated approver** set by the administrator. Nobody approves their own timesheet. |
| Return | Any step can return it, to the **employee**, with a message. After the correction it **starts again at the first step** (confirmed earlier in the analysis). |
| Frozen chain | The chain in force when the timesheet is submitted is stored with that submission, so a later change in the organisation chart does not alter a running approval. |
| Absence of a supervisor | A **deputy** can be named per supervisor for a period; the audit log shows who acted and for whom. |
| Visibility | A supervisor sees the timesheets of the people below them in the chain and nobody else's. Health-related codes are visible to the chain, the lawyer and finance (Q-25). |
| Finance | Not an approval step for employees' timesheets (it receives approved data later, Phase 2); it only approves what sits in its own chain, such as the lawyer's timesheet. |

This replaces the fixed "employee, service head, lawyer" path of the analysis (`HEAD` and `LEG` become positions in the
chain, with the lawyer flagged as final approver) and is the "chained flow" the stakeholders described in Q-11.

## 7. What carries over and what is new

| Area | Needed by the analysis | Reuse from today |
| --- | --- | --- |
| Rules: overtime split at 06:00, 08:30, 17:00, 22:00 and midnight, classification into codes 2020 to 2025, monthly fund, absence ranges that skip weekends | CALC-01 to 07, BR-17 to 23 | Pure code with tests, new, small. The sample sheet and the instruction's examples are the fixtures. |
| Validation: 21 rules with ids and severities | V-01 to V-21 | New, pure code |
| Status machine, revisions with hash, audit | T1 to T12 | New |
| Month grid, absence by range, overtime register, review screens | Requirements section 5 | New screens in the existing design system; the week view patterns help |
| Reminders | By the 25th, approvals before next month | Reuse the task and tray notifier (section 4) |
| Printed sheet in the Excel layout | Q-33: required, landscape A4, colour rows, 9 to 10 rows per employee | QuestPDF and ClosedXML are in the solution; medium effort |
| Bosnian interface | Q-32: Bosnian first | Existing catalogue and language switch |

## 8. Proposed solution layout

```
src/DailyTaskManager.Core            existing
src/DailyTaskManager.Infrastructure  existing (consultant data access, exports)
src/DailyTaskManager.Avalonia        existing desktop app; gains Employee mode and an API client
src/Timesheet.Domain                 new: calendar, overtime, fund, validation, workflow (no I/O)
src/Company.Api                      new: ASP.NET Core API, identity (AD / Entra), timesheet logic and data
src/Company.Api.Contracts            new: the request and response types shared by the API and the desktop app
tests/Timesheet.Domain.Tests         new: fixtures from the sample sheet and the instruction
tests/Company.Api.Tests              new: authorisation and workflow tests
```

## 9. Gaps and contradictions found in the analysis

1. **Approval chain.** Settled in section 6: a chain of any length through each person's supervisor. Still to
   confirm: the default for the person at the top, and that finance approves only its own chain. Q-10 says "the
   administrator approves": with the chain, that is simply the next supervisor.
2. **Extra master data.** Q-13 says only code and name are kept, but the workflow needs the supervisor and
   Windows sign-in needs the account. See section 3b for how to link them without typing 112 mappings.
3. **Holidays per employee.** Q-14: state and religious holidays are marked per employee and nobody maintains
   them today. The analysis models one global calendar. This needs a yearly holiday list with an administrator and a
   per-employee assignment for religious holidays. Q-03: holiday codes apply even when the holiday is on a weekend.
4. **Overtime on absence days (Q-35).** Allowed on annual leave, not on sick leave, not on business trips.
5. **Hours in "units of an hour" (Q-17)** while the sample has 2.5 h. Store minutes; make the granularity a setting.
6. **Shift workers are excluded for now (Q-34).** Codes 2011 and 2012 can stay hidden. Q-16: employees add rows only
   for codes they use, so the grid shows the used rows and an "add row" action.
7. **The printed sheet (Q-33) is required** but the analysis schedules it late (M6). It belongs in the first release.
8. **Deadline:** the 25th, "moved to the first working day" on a weekend (Q-21); the direction is ambiguous.
9. **Employee list:** people are missing and will be added later, so add-employee must exist from the start. One
   entry has stray spaces in the code and the sample sheet has `Z- 255`.
10. **The legal reviewer checks nothing beyond the instruction (Q-06)**, so no legal-limit rules are needed.
11. **Finance is out of the first release (Q-04, Q-05 open).** Approved data only needs a read-only view or simple export.
12. **The sample sheet carries the Logosoft name and logo**, so this product serves your client's company. Their IT
    hosts the API, owns the employee data and decides how consultants' data is treated (section 2).

## 10. Suggested order of work

| Step | Content | Depends on |
| --- | --- | --- |
| 1 (done) | `Timesheet.Domain` with tests: calendar, overtime split and classification, fund, absence ranges, validation rules. | Nothing external |
| 2 | `Company.Api` skeleton: Windows sign-in with a test mode, the Entra option behind a setting, user profiles and roles, employee import, time-code catalogue, holiday administration, audit base. | Test domain (section 3) |
| 3 | Employee mode in the desktop app (mode choice at sign-in, Windows sign-in): month grid, absence by range, overtime register, validation report, submit. | 1, 2 |
| 4 | Workflow: submit, return with message, approval up the chain (section 6), locking, frozen revisions, history. | Section 6 defaults |
| 5 | Reminders from the API: rules, the scheduled task asking the API, notifications. | 2, 4 |
| 6 | Printed sheet in the Excel layout, pilot with a parallel run, go-live. | 1 to 5 |

Phase 2 as in the analysis: finance hand-over, reports, legacy import.

## 11. Decisions needed

1. Confirm the defaults of section 6: the person at the top gets a designated approver; finance approves only its own
   chain; deputies are wanted.
2. A Windows server in the company network for the API, and a test domain or test server (section 3b).
3. Go-ahead to start with step 1 (domain library with tests), which needs none of the answers above.

## 12. Step 1 result: `Timesheet.Domain`

Implemented in `src/Timesheet.Domain` (pure .NET 8 library, no dependencies, no I/O) with 96 tests in
`tests/Timesheet.Domain.Tests`. The tests run in the existing "Build and Test" workflow.

| Part | What it does |
| --- | --- |
| `TimeCodes` | The catalogue of the legend with unit, colour and fund membership |
| `WorkCalendar`, `WorkingTimeSettings` | Generated days, state holidays, per-employee religious holidays, Bosnian weekday letters, next working day; the working-time rules as data |
| `OvertimeClassifier` | Splits an interval at 06:00, 08:30, 17:00, 22:00 and midnight and gives each part its code (2020 to 2025) by the type of its date |
| `AbsenceRanges` | Expands "code, from, to" into days and reports the days it leaves out |
| `MonthlySummaryCalculator` | Hours and days per code, the monthly fund, overtime total, expected fund |
| `TimesheetValidator` | The checks V-01 to V-21 with configurable severities and Bosnian messages |
| `ApprovalChainBuilder`, `TimesheetWorkflow` | The chain of any length, and submit, approve, return, answer with the rules of section 6; every transition returns an audit event |
| `Deadlines`, `ReminderRules` | The 25th (next working day if not working), approval before the next month, who is due a reminder |
| `EmployeeListImporter`, `EmployeeCode` | The employee list preview: bad codes, duplicates, trimmed names, existing codes with another name |

**Acceptance tests that pass:** the six overtime records of the sample sheet give exactly the allocations of the
sample; the examples of the instruction (Friday 17:00 to 21:00; Saturday 21:00 to 02:00 with the Sunday part; a
partly worked holiday) are reproduced; the eleven sample employees give the derived summary (1,480 regular hours,
35 annual leave days, 8.0 h of 2020, 1.5 h of 2021, 5.0 h of 2023) and a monthly fund of 176 h each; entering
annual leave 01.09 to 15.09 gives exactly 11 days; the real employee list imports as 112 rows, 1 trimmed name and
0 errors (checked with a local path, the list is not in the repository).

**Findings while building it:**

1. The legend has **22 codes**, not 20 as the analysis says.
2. **A weekend has no regular window.** Weekend overtime from 06:00 to 22:00 is one code (2023), also between 08:30
   and 17:00. The sample proves it (Saturday 08:30 to 11:00 is 2023 = 2.5). Overtime inside 08:30 to 17:00 is only
   an error on a working day or a holiday.
3. Rules changed by the stakeholder answers: V-17 is blocking (Q-08); V-21 warns only when the fund is **lower** than
   expected (Q-36); overtime is allowed on annual leave, **blocked on sick leave (V-19b)** and a warning on the other
   absences (V-19c, undecided); V-15 means "start equals end" because overtime past midnight is allowed (Q-22).
4. The sample sheet entered as it is would be rejected for employee Z-251, whose maternity leave sits on weekends.

**Assumptions to confirm** (each is one setting or one line of code):

- Hours are kept in minutes; the step is half an hour (V-12 warns). Q-17 says "in units of an hour", but the sample
  has 2.5 h.
- Range absences skip the employee's religious holidays (they have code 1025); state holidays use 1024 and 2010.
- When the 25th is not a working day the deadline moves **later**; reminders start three days before the deadline.
- A holiday on a weekend uses holiday codes, also for a religious holiday (Q-03).
