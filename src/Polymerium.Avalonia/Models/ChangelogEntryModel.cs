using System.Collections.Generic;

namespace Polymerium.Avalonia.Models;

public sealed record ChangelogEntryModel(
    string Markdown,
    IReadOnlyList<ChangelogReferenceModel> References);
