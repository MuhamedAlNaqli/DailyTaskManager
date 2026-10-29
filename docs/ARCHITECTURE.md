# Architecture

Daily Task Manager is a Windows desktop app (.NET 8, Avalonia, PostgreSQL). The solution has three layers and
dependencies only point downwards: `Avalonia` -> `Infrastructure` -> `Core`.

```
src/
  DailyTaskManager.Core            netstandard2.0   what the app is about, no framework code
  DailyTaskManager.Infrastructure  net8.0           how it talks to the outside world
  DailyTaskManager.Avalonia        net8.0           what the user sees and the Windows-specific parts
tests/
  DailyTaskManager.UI.Tests        MSTest: integration tests against the real services, plus FlaUI UI tests
```

## Core

Models (`DailyTask`, `Client`, `TaskTemplate`, `TaskQuery`, analytics and backup models), service contracts
(`Interfaces/I*Service`) and pure rules that need no I/O:

- `Reminders/MissingDays` finds working days without hours.
- `Reminders/ReminderEvaluator` combines the settings and the user's entries into one answer ("is a reminder due,
  and what should it say?"). The in-app banner and the background job both use it, so they never disagree.

Core has no reference to EF Core, Avalonia or Windows APIs, which keeps the rules easy to test.

## Infrastructure

Implementations of the Core contracts: `DatabaseService` (EF Core + Npgsql, one short-lived `DbContext` per
operation through `IDbContextFactory`), `SettingsService`, `AuthenticationService` and `SessionStore`
("remember me"), `ExportService` (PDF, Word, Excel, CSV, invoices), `BackupService` (JSON backup, CSV/Excel import),
`SpeechRecognitionService`. The schema is upgraded automatically at startup (`DailyTaskManagerDbContext`).

## Avalonia (UI)

MVVM with CommunityToolkit.Mvvm. A view (`Views/*.axaml`) binds to one view model (`ViewModels/*`); view models
talk to Core interfaces only. Views contain no business logic, only UI glue such as focus and key handling.

| Folder | Contents |
| --- | --- |
| `Views` | Windows, dialogs and pages (`MainWindow`, `DailyTasksView`, `AnalyticsView`, ...) |
| `ViewModels` | One view model per view, created through dependency injection |
| `Services` | UI services: navigation, dialogs, updates, dictation, single-instance, memory trimming |
| `Background` | Windows scheduled reminder (see below) |
| `Styles`, `App.axaml` | Design system: colour tokens, typography, control styles |

The Daily Tasks page has two views, switched in its header: the **week view** (default) shows seven day columns,
Monday to Sunday, with previous/next week and "This week" navigation and an add button per day; the **list view**
keeps the filters, paging and bulk actions. Both read the same data, so every change refreshes both
(`DailyTasksViewModel.Week.cs`, `WeekDay`).

Dependency injection is configured in `Program.CreateHostBuilder`. Startup is ordered so the window appears
quickly: the database is initialised on a worker thread while Avalonia starts, and non-essential work (update
check, reminder registration) runs in the background.

## Design system

- **Colour**: one brand colour, deep teal in the light theme and bright teal in the dark theme, defined once as
  tokens (`AppAccent`, `AppAccentSoft`, `AppSurface`, ...) in `App.axaml`. Views use `{DynamicResource App...}`
  and never hard-code colours (the exceptions are semantic chart colours: green, amber and red).
- **Typography**: Inter, set globally in `Styles/App.axaml`.
- **Icons**: Material Design icons through `Material.Icons.Avalonia` (`<materialIcons:MaterialIcon Kind="..."/>`);
  no emoji in the UI.
- **Controls**: Material.Avalonia supplies the control templates; the Material primary colour is overridden by the
  brand tokens so nothing purple remains. Button classes: `primary`, `secondary`, `danger`, `icon`.

## Background reminder

The reminder works while the app is closed without a resident process:

1. On startup (and whenever the settings change) the app registers a Windows scheduled task,
   `DailyTaskManager\Reminder`, that runs the app's own exe with `--reminder-check` at logon and every 30 minutes
   (`Background/WindowsReminderScheduler`). The registration is repeated only if the install folder changed.
2. `Program.Main` recognises the argument and runs `Background/ReminderRunner` instead of the UI: it restores the
   saved "remember me" session (without extending its expiry), asks `ReminderEvaluator`, and if a reminder is due
   shows a Windows notification (`Background/TrayNotifier`, plain Win32, no UI toolkit). Clicking the
   notification starts the app, or brings the running window to the front (`Services/SingleInstance`).
3. `Background/ReminderState` (in `%LOCALAPPDATA%\DailyTaskManager`) limits the reminder to twice a day, at least
   two hours apart.

The process lives for a second or two (up to 30 s while the notification is on screen) and then exits.
Settings: Settings -> Reminders (on/off, hour, "also remind me when the app is closed", test notification).
The job only works for a user who chose "remember me", because the reminder needs to know whose entries to check.
Uninstalling the app leaves the (harmless) scheduled task behind; remove it with
`schtasks /Delete /TN "DailyTaskManager\Reminder" /F` or by switching the setting off before uninstalling.

## Memory

The app keeps its working set small on purpose: software rendering (no GPU driver in the process), workstation
non-concurrent GC, ReadyToRun code, no unused packages, and `MemoryTrimmer` returns unused pages to Windows after
startup and when the window is minimized.
