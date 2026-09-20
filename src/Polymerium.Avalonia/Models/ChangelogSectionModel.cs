using System.Collections.Generic;

namespace Polymerium.Avalonia.Models;

public sealed record ChangelogSectionModel(
    ChangelogSectionKind Kind,
    IReadOnlyList<ChangelogEntryModel> Entries);
