using System.Collections.Generic;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class LoaderCompoundModel(
    string loaderId,
    string displayName,
    IReadOnlyList<string> versions,
    string? recommendedVersion) : ModelBase
{
    #region Direct

    public string LoaderId => loaderId;
    public string DisplayName => displayName;
    public IReadOnlyList<string> Versions => versions;

    public string? RecommendedVersion => recommendedVersion;

    #endregion
}
