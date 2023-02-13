using System.Collections.Generic;
using System.Linq;

namespace Polymerium.Core.Components;

public class ComponentMeta
{
    public ComponentMeta(
        string identity,
        string friendlyName,
        IEnumerable<string>? dependencies = null
    )
    {
        Identity = identity;
        FriendlyName = friendlyName;
        Dependencies = dependencies ?? Enumerable.Empty<string>();
    }

    public string Identity { get; set; }
    public string FriendlyName { get; set; }
    public IEnumerable<string> Dependencies { get; set; }
}
