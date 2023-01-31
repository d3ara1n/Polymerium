using System.Collections.Generic;
using System.Linq;
using Polymerium.Core.Components;

namespace Polymerium.App.Services;

public enum ComponentViewFilter
{
    All,
    Core,
    Modloader
}

public class ComponentManager
{
    private readonly MemoryStorage _memoryStorage;

    public ComponentManager(MemoryStorage memoryStorage)
    {
        _memoryStorage = memoryStorage;
        // GameInstance.Meta 中的 components 依赖关系的图顶点只能有一个元素, {fabric,forge}->{minecraft} 中fabric forge平行那么即互斥
        _memoryStorage.SupportedComponents = new List<ComponentMeta>
        {
            new()
            {
                Identity = "net.minecraft",
                FriendlyName = "Minecraft"
            },
            new()
            {
                Identity = "net.minecraftforge",
                FriendlyName = "Forge",
                Dependencies = new[] { "net.minecraft" }
            },
            new()
            {
                Identity = "net.fabricmc.fabric-loader",
                FriendlyName = "Fabric",
                Dependencies = new[] { "net.minecraft" }
            },
            new()
            {
                Identity = "org.quiltmc.quilt-loader",
                FriendlyName = "Quilt",
                Dependencies = new[] { "net.minecraft" }
            }
        };
    }

    public IEnumerable<ComponentMeta> GetView()
    {
        return GetView(ComponentViewFilter.All);
    }

    public bool TryFindByIdentity(string identity, out ComponentMeta meta)
    {
        meta = _memoryStorage.SupportedComponents.FirstOrDefault(x => x.Identity == identity);
        return meta != null;
    }

    public IEnumerable<ComponentMeta> GetView(ComponentViewFilter filter)
    {
        return filter switch
        {
            ComponentViewFilter.All => _memoryStorage.SupportedComponents,
            ComponentViewFilter.Core => _memoryStorage.SupportedComponents.Where(x =>
                x.Dependencies == null || !x.Dependencies.Any()),
            ComponentViewFilter.Modloader => _memoryStorage.SupportedComponents.Where(x =>
                x.Dependencies != null && x.Dependencies.Any())
        };
    }
}