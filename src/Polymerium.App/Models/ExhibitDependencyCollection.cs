using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.App.Models
{
    public class ExhibitDependencyCollection(string versionName, string versionId, IList<ExhibitDependencyModel> items)
        : Collection<ExhibitDependencyModel>(items)
    {
        public string VersionName => versionName;
        public string VersionId => versionId;
    }
}
