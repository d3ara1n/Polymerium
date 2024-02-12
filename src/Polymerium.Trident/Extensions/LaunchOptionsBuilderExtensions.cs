using Polymerium.Trident.Launching;

namespace Polymerium.Trident.Extensions;

public static class LaunchOptionsBuilderExtensions
{
    public static LaunchOptionsBuilder FireAndForget(this LaunchOptionsBuilder self)
    {
        return self.SetMode(LaunchMode.FireAndForget);
    }

    public static LaunchOptionsBuilder Managed(this LaunchOptionsBuilder self)
    {
        return self.SetMode(LaunchMode.Managed);
    }
}