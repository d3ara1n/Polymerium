using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.Avalonia.Models;

public class InstancePackageVersionCollection(IList<InstancePackageVersionModelBase> items)
    : Collection<InstancePackageVersionModelBase>(items);
