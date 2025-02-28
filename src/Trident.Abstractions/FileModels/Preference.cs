namespace Trident.Abstractions.FileModels;

public record Preference(
    Preference.AccountSection Account,
    Preference.LaunchSection Launch,
    Preference.SnapshotSection Snapshot,
    Preference.PersistSection Persist)
{
    #region Nested type: AccountSection

    public record struct AccountSection(string Uuid);

    #endregion

    #region Nested type: EntryCollection

    public record struct EntryCollection(IEnumerable<string> Files, IEnumerable<string> Folders);

    #endregion

    #region Nested type: LaunchSection

    public record struct LaunchSection(LaunchMode Mode, string? JavaHome);

    #endregion

    #region Nested type: PersistSection

    public record struct PersistSection(EntryCollection Entries);

    #endregion

    #region Nested type: SnapshotSection

    public record struct SnapshotSection(EntryCollection Entries);

    #endregion
}