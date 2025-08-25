namespace Polymerium.Trident.Models.XboxLiveApi
{
    public readonly record struct MinecraftTokenProperties(
        IReadOnlyList<string> UserTokens,
        string SandboxId = "RETAIL");
}
