using System.Collections.Generic;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models
{
    public class LoaderCandidateVersionCollectionModel(IReadOnlyList<LoaderCandidateVersionModel> versions) : ModelBase
    {
        #region Direct

        public IReadOnlyList<LoaderCandidateVersionModel> Versions => versions;

        #endregion
    }
}
