using Polymerium.App.Facilities;
using TridentCore.Core.Utilities;

namespace Polymerium.App.Models;

public class RuntimePickerDialogCandidateModel(JavaHelper.JavaRuntimeCandidate candidate)
{
    public string Home => candidate.Home;
    public string? Vendor => candidate.Vendor;
    public string? Version => candidate.Version;
    public int? Major => candidate.Major;
    public string Source => candidate.Source;
}
