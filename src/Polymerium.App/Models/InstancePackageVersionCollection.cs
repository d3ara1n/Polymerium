using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.App.Models
{
    public class InstancePackageVersionCollection(IList<InstancePackageVersionModelBase> items)
        : Collection<InstancePackageVersionModelBase>(items);
}
