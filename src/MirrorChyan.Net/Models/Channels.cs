namespace MirrorChyan.Net.Models;

public static class Channels
{
    public const string STABLE = "stable";
    public const string BETA = "beta";
    public const string ALPHA = "alpha";

    public static string FromKind(ChannelKind? kind)
    {
        return kind switch
        {
            ChannelKind.Stable => STABLE,
            ChannelKind.Beta => BETA,
            ChannelKind.Alpha => ALPHA,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public static ChannelKind ToKind(string channel)
    {
        return channel switch
        {
            STABLE => ChannelKind.Stable,
            BETA => ChannelKind.Beta,
            ALPHA => ChannelKind.Alpha,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
        };
    }
}
