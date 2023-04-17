using System.Collections.Generic;
using System.Linq;

namespace Polymerium.Core.Components;

public class ComponentMeta
{
    public const string MINECRAFT = "net.minecraft";
    public const string FORGE = "net.minecraftforge";
    public const string FABRIC = "net.fabricmc.fabric-loader";
    public const string QUILT = "org.quiltmc.quilt-loader";

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
