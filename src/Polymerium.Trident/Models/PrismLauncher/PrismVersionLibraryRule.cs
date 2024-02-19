namespace Polymerium.Trident.Models.PrismLauncher
{
    public struct PrismVersionLibraryRule
    {
        public PrismVersionLibraryRuleAction Action { get; init; }

        /// Typically name, arch, version
        public IDictionary<string, string>? Os { get; init; }
    }
}