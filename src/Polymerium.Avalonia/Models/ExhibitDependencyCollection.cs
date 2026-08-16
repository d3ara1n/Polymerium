using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.Avalonia.Models;

public class ExhibitDependencyCollection(string versionName,
                                         string versionId,
                                         IList<ExhibitDependencyModel> items,
                                         int missingRequiredCount)
    : Collection<ExhibitDependencyModel>(items)
{
    public string VersionName => versionName;

    public string VersionId => versionId;

    public int MissingRequiredCount => missingRequiredCount;
}
