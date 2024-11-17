namespace Trident.Abstractions.FileModels;

public record struct Preference(
    Preference.AccountSection Account,
    Preference.LaunchSection Launch,
    Preference.SnapshotSection Snapshot,
    Preference.PersistSection Persist)
{
    public record struct EntryCollection(IEnumerable<string> Files, IEnumerable<string> Folders);

    public record struct LaunchSection(LaunchMode Mode, string? JavaHome);

    public record struct AccountSection(string Uuid);

    public record struct SnapshotSection(EntryCollection Entries);

    public record struct PersistSection(EntryCollection Entries);
}