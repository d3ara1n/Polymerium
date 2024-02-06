namespace Polymerium.Trident.Models.Minecraft;

public struct MinecraftVersionArguments
{
    public MinecraftVersionArgument[] Game { get; init; }
    public MinecraftVersionArgument[] Jvm { get; init; }
}