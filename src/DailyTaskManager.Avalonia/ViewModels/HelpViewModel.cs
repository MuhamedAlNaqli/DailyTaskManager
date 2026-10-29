using System.Collections.Generic;
using Material.Icons;

namespace DailyTaskManager.Avalonia.ViewModels;

/// <summary>One tip: optional key combination(s) plus what they do</summary>
public record HelpItem(string Title, string Description, params string[] Keys)
{
    public bool HasKeys => Keys.Length > 0;
}

/// <summary>A titled group of tips</summary>
public record HelpSection(string Title, string Summary, MaterialIconKind Icon, IReadOnlyList<HelpItem> Items);

/// <summary>
/// The Help page: everything that is easy to miss (shortcuts, double-click editing, background notices) in one place.
/// Plain data, so the content is easy to keep in step with the features.
/// </summary>
public class HelpViewModel
{
    public IReadOnlyList<HelpSection> Sections { get; } = BuildSections();

    private static IReadOnlyList<HelpSection> BuildSections() => new[]
    {
        new HelpSection("Keyboard shortcuts", "Work faster without the mouse", MaterialIconKind.KeyboardOutline, new[]
        {
            new HelpItem("New entry", "Opens the Add Entry dialog from the Daily Tasks page.", "Ctrl", "N"),
            new HelpItem("Search", "Jumps to the search box in the list view and selects what is typed there.", "Ctrl", "F"),
            new HelpItem("Undo a delete", "Brings back the entry you just deleted. The Undo bar at the bottom stays for about 8 seconds; the shortcut works while it is shown.", "Ctrl", "Z"),
            new HelpItem("Save an inline edit", "Saves the description you are editing in place. Plain Enter adds a new line there, because descriptions can have several lines.", "Ctrl", "Enter"),
            new HelpItem("Save hours", "Saves the hours you are editing in place.", "Enter"),
            new HelpItem("Cancel", "Discards an inline edit without saving.", "Esc"),
        }),

        new HelpSection("Daily Tasks", "Two ways to look at the same entries", MaterialIconKind.ClipboardCheckOutline, new[]
        {
            new HelpItem("Week view", "The default. Seven columns, Monday to Sunday, each with its entries and the day's hours. Use the arrow buttons to move between weeks and \"This week\" to jump back. Today is outlined."),
            new HelpItem("List view", "Switch with the Week / List buttons in the header. The list adds search, date range and client filters, paging and bulk actions."),
            new HelpItem("Add for a day", "Press Add at the bottom of a day column to create an entry for that exact day."),
            new HelpItem("Edit an entry", "Click the text of an entry in the week view, or use the pencil in the list view."),
            new HelpItem("Edit in place", "In the list view, double-click a description or the hours badge to edit it right there. Click away to save, or press Esc to cancel.", "Double-click"),
            new HelpItem("Duplicate", "The copy button opens the Add dialog pre-filled with that entry, dated today. Handy for recurring work."),
            new HelpItem("Delete and undo", "Deleting does not ask for confirmation. Use the Undo button in the bar that appears, or press Ctrl+Z."),
            new HelpItem("Quick add", "The Quick add button lists your saved templates. One click creates an entry from a template."),
            new HelpItem("Assign a client in bulk", "In the list view, filter the entries you want and press Assign client to set (or remove) the client of all of them at once."),
            new HelpItem("Statistics", "The cards at the top show today, this week (Monday to Sunday), all entries and your 30-day average against the 7 hour target."),
        }),

        new HelpSection("Adding and editing an entry", "What the entry dialog can do", MaterialIconKind.PlaylistEdit, new[]
        {
            new HelpItem("Description suggestions", "While you type, earlier descriptions are suggested. Use the Up and Down arrows with Enter to pick one, or Esc to hide the list.", "Up", "Down", "Enter"),
            new HelpItem("Dictate", "The Dictate button turns your voice into the description (shown when a microphone is available). Speak in phrases: each pause adds the text spoken so far. It runs offline with the Whisper model, which downloads once (about 190 MB) the first time. Pick your language under Settings, Dictation for the best accuracy.", "Dictate"),
            new HelpItem("Clients", "Pick an existing client, or type a new name in the second field to create it with the entry."),
            new HelpItem("Templates", "Tick \"Save as a template for quick add\" to reuse an entry later. A saved template can also be picked at the top of the dialog."),
        }),

        new HelpSection("Calendar", "See which days are missing hours", MaterialIconKind.CalendarMonthOutline, new[]
        {
            new HelpItem("Colours", "Green days have 7 hours or more, amber days less than that, and days marked as missing are working days without an entry."),
            new HelpItem("Click a day", "Adds an entry for that day, or opens the existing entry for editing."),
        }),

        new HelpSection("Reports and exports", "Hand your hours to someone else", MaterialIconKind.FileDocumentOutline, new[]
        {
            new HelpItem("Work order", "Choose a month and client in Reports, then export a PDF, Word or Excel work order."),
            new HelpItem("Invoice", "Fills the hours of the chosen month into an invoice with your rate, VAT and payment note."),
            new HelpItem("Export locations", "Settings lets you choose a separate folder for PDF, Word and Excel files."),
            new HelpItem("Company details", "Your header and client details for the documents are kept under Settings, PDF Export Customization."),
        }),

        new HelpSection("Reminders and updates", "Notices that work even when the app is closed", MaterialIconKind.BellOutline, new[]
        {
            new HelpItem("Missing hours banner", "A banner at the top reminds you when a working day has no entry, from the hour you choose in Settings."),
            new HelpItem("Windows notifications", "With \"remind me when the app is closed\" on, Windows checks every 30 minutes and shows a notification (at most twice a day). It needs \"remember me\" at sign-in, so it knows whose hours to check."),
            new HelpItem("Test a notification", "Settings, Reminders, \"Show a test notification\" shows exactly what you will get."),
            new HelpItem("New versions", "Windows also checks for a new version a few times a day and notifies you once per release. Clicking the notice opens the app, which offers the update. Turn it off under Settings, Updates."),
            new HelpItem("Check now", "Settings, Updates, \"Check for Updates\" looks right away."),
        }),

        new HelpSection("Your data and the app", "Backups, appearance and troubleshooting", MaterialIconKind.ShieldCheckOutline, new[]
        {
            new HelpItem("Backup and restore", "Settings, Backup and import, saves everything to a file and restores it. Restoring never deletes anything and skips entries that already exist."),
            new HelpItem("Import", "Entries can be imported from CSV or Excel files that have Date, Description and Hours columns. The app's own exports work."),
            new HelpItem("Light and dark", "The theme button at the bottom of the sidebar switches between them; the choice is remembered."),
            new HelpItem("Language and dictation", "Interface language, and the language you speak when dictating, are chosen separately in Settings."),
            new HelpItem("Log files", "If something goes wrong, the log is in %LOCALAPPDATA%\\DailyTaskManager\\logs. Send it along when you report a problem."),
        }),
    };
}
