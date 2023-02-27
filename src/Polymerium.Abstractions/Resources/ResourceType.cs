using System;

namespace Polymerium.Abstractions.Resources;

[Flags]
public enum ResourceType : uint
{
    Modpack,
    Mod,
    ResourcePack,
    DataPack,
    World,
    Shader,
    Plugin
}