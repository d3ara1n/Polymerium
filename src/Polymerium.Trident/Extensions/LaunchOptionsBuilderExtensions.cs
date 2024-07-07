using Polymerium.Trident.Launching;

namespace Polymerium.Trident.Extensions;

public static class LaunchOptionsBuilderExtensions
{
    public static LaunchOptionsBuilder FireAndForget(this LaunchOptionsBuilder self) =>
        self.WithMode(LaunchMode.FireAndForget);

    public static LaunchOptionsBuilder Managed(this LaunchOptionsBuilder self) => self.WithMode(LaunchMode.Managed);
}