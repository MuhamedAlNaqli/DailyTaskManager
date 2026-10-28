using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Avalonia.ViewModels;

/// <summary>
/// An entry in a client drop-down (a real client or one of the special choices)
/// </summary>
public class ClientChoice
{
    /// <summary>Filter choice: do not filter by client</summary>
    public static readonly ClientChoice All = new(null, "All clients");

    /// <summary>Filter choice: only entries without a client</summary>
    public static readonly ClientChoice Unassigned = new(TaskQuery.Unassigned, "Unassigned");

    /// <summary>Dialog choice: the entry has no client</summary>
    public static readonly ClientChoice NoClient = new(null, "No client");

    public ClientChoice(int? id, string name)
    {
        Id = id;
        Name = name;
    }

    public int? Id { get; }

    public string Name { get; }

    public static ClientChoice From(Client client) => new(client.Id, client.Name);

    public override string ToString() => Name;
}
