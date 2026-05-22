using System.Collections.Generic;

namespace Polymerium.App.Models;

public record FilePartitionModel(string Label, int Count, long Size, IReadOnlyList<FileCategoryEntryModel> Categories);
