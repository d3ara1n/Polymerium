using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class LoaderCandidateSelectionModel(string id, string version) : ModelBase
{
    #region Direct

    public string Id => id;
    public string Version => version;

    #endregion
}
