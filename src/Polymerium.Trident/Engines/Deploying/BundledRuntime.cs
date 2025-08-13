namespace Polymerium.Trident.Engines.Deploying
{
    // PrismLauncher 里的下下来压缩包内部包了一层，因此叫 Nested
    public record BundledRuntime(uint Major, string Vendor, Uri Url, bool Nested);
}
