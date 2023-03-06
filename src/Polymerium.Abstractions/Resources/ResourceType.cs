using System;

namespace Polymerium.Abstractions.Resources;

[Flags]
public enum ResourceType : uint
{
    None = 0,
    Modpack = 0x1,
    Mod = 0x10,
    ResourcePack = 0x100,
    DataPack = 0x1000,
    World = 0x10000,
    Shader = 0x100000,
    Plugin = 0x1000000,
    File = 0x10000000,
    All = Modpack | Mod | ResourcePack | DataPack | World | Shader | Plugin | File
}