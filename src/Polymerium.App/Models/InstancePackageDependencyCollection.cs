using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.App.Models;

public class InstancePackageDependencyCollection(IList<InstancePackageDependencyModel> items)
    : Collection<InstancePackageDependencyModel>(items) { }