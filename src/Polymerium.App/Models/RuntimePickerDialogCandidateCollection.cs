using System.Collections.Generic;
using System.Linq;
using TridentCore.Core.Utilities;

namespace Polymerium.App.Models;

public class RuntimePickerDialogCandidateCollection(
    IReadOnlyList<JavaHelper.JavaRuntimeCandidate> candidates
)
{
    public IReadOnlyList<RuntimePickerDialogCandidateModel> Items { get; } =
    [.. candidates.Select(x => new RuntimePickerDialogCandidateModel(x))];

    public int Count => Items.Count;
}
