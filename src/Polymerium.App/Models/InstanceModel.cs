using System.Collections.Generic;
using Polymerium.Abstractions;

namespace Polymerium.App.Models;

public class InstanceModel
{
    public GameInstance Inner { get; set; }
    public string InstanceName => Inner.Name;
    public IEnumerable<ComponentTagItemModel> Extenders { get; set; }
}