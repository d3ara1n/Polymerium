using System;

namespace Polymerium.Abstractions.Resources;

[Flags]
public enum ResourceType : uint
{
    None = 0,
    Modpack = 0b1,
    Mod = 0b10,
    ResourcePack = 0b100,
    DataPack = 0b1000,
    World = 0b10000,
    ShaderPack = 0b100000,
    Plugin = 0b1000000,
    Update = 0b10000000,
    File = 0b100000000,
    All = Modpack | Mod | ResourcePack | DataPack | World | ShaderPack | Plugin | Update | File
}