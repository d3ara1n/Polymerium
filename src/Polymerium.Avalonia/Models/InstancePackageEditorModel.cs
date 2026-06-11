using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class InstancePackageEditorModel(InstancePackageModel entry) : ModelBase
{
    #region Direct

    public InstancePackageModel Entry => entry;

    #endregion
}
