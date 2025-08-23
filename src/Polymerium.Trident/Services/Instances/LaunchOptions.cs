using Polymerium.Trident.Igniters;
using Trident.Abstractions.Accounts;

namespace Polymerium.Trident.Services.Instances
{
    public class LaunchOptions(
        string? brand = null,
        LaunchMode launchMode = LaunchMode.Managed,
        IAccount? account = null,
        (uint, uint)? windowSize = null,
        uint maxMemory = 4096,
        string? additionalArguments = null)
    {
        public LaunchMode Mode { get; set; } = launchMode;

        public IAccount? Account { get; set; } = account;
        public uint MaxMemory { get; set; } = maxMemory;
        public (uint, uint) WindowSize { get; set; } = windowSize ?? (1270, 720);
        public string AdditionalArguments { get; set; } = additionalArguments ?? string.Empty;

        public string Brand { get; set; } = brand ?? "Trident";
    }
}
