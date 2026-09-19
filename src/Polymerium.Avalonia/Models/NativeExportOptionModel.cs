using System.Collections.Generic;

namespace Polymerium.Avalonia.Models;

public class NativeExportOptionModel : ExportOptionModel
{
    public required IReadOnlyList<ReleaseFileModel> ReleaseFiles { get; init; }
}
