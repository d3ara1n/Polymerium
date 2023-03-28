using System;
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
            new(ComponentMeta.MINECRAFT, "Minecraft"),
            new(ComponentMeta.FORGE, "Forge", new[] { ComponentMeta.MINECRAFT }),
            new(ComponentMeta.FABRIC, "Fabric", new[] { ComponentMeta.MINECRAFT }),
            new(ComponentMeta.QUILT, "Quilt", new[] { ComponentMeta.MINECRAFT })
        };
    }

    public string? ToFriendlyName(string identity)
    {
        if (TryFindByIdentity(identity, out var meta))
            return meta!.FriendlyName;
        return null;
    }

    public bool TryFindByIdentity(string identity, out ComponentMeta? meta)
    {
        meta = _memoryStorage.SupportedComponents.FirstOrDefault(x => x.Identity == identity);
        return meta != null;
    }

    public IEnumerable<ComponentMeta> GetView(ComponentViewFilter filter = ComponentViewFilter.All)
    {
        return filter switch
        {
            ComponentViewFilter.Core
                => _memoryStorage.SupportedComponents.Where(x => !x.Dependencies.Any()),
            ComponentViewFilter.Modloader
                => _memoryStorage.SupportedComponents.Where(x => x.Dependencies.Any()),
            ComponentViewFilter.All => _memoryStorage.SupportedComponents,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
    }
}