using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Polymerium.App.Models;

public class InstancePackageDependencyCollection(IList<InstancePackageDependencyModel> items)
    : ReadOnlyCollection<InstancePackageDependencyModel>(items)
{
    public uint StrongCount { get; } = (uint)items.Count(x => x.IsRequired);
}
