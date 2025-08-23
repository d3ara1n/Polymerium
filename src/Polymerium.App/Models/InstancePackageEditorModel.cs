using Polymerium.App.Facilities;

namespace Polymerium.App.Models
{
    public class InstancePackageEditorModel(InstancePackageModel entry) : ModelBase
    {
        #region Direct

        public InstancePackageModel Entry => entry;

        #endregion
    }
}
