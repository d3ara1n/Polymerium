using System.Collections.Generic;

namespace Polymerium.Avalonia.Models;

public sealed record ChangelogDocumentModel(
    IReadOnlyList<ChangelogEntryModel> Highlights,
    IReadOnlyList<ChangelogSectionModel> Sections);
