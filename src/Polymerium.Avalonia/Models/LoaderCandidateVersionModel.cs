using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class LoaderCandidateVersionModel(string version, bool isRecommanded) : ModelBase
{
    #region Direct

    public string Version => version;

    public bool IsRecommended => isRecommanded;

    #endregion
}
