using System.Collections.Generic;

namespace Polymerium.Avalonia.Models;

public record FilePartitionModel(string Label, int Count, long Size, IReadOnlyList<FileCategoryEntryModel> Categories);
