using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class LoaderCandidateVersionModel(string version, bool isRecommanded) : ModelBase
{
    #region Direct

    public string Version => version;

    public bool IsRecommended => isRecommanded;

    #endregion
}