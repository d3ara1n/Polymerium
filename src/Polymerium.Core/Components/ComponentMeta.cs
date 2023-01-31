using System.Collections.Generic;

namespace Polymerium.Core.Components;

public class ComponentMeta
{
    public string Identity { get; set; }
    public string FriendlyName { get; set; }
    public IEnumerable<string> Dependencies { get; set; }
}