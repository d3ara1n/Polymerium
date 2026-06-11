using System.Collections.Generic;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class LoaderCandidateVersionCollectionModel(
    IReadOnlyList<LoaderCandidateVersionModel> versions
) : ModelBase
{
    #region Direct

    public IReadOnlyList<LoaderCandidateVersionModel> Versions => versions;

    #endregion
}
