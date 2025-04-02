using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class LoaderCandidateVersionModel(string version) : ModelBase
{
    #region Direct

    public string Version => version;

    #endregion
}