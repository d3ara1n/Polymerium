using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.Avalonia.Models;

public class InstanceActionCollection(IList<InstanceActionModel> list)
    : Collection<InstanceActionModel>(list);
