using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.App.Models;

public class InstancePackageModificationCollection(IList<InstancePackageModificationModel> list)
    : Collection<InstancePackageModificationModel>(list);
