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
            ChannelKind.Beta => BETA,
            ChannelKind.Alpha => ALPHA,
            _ => STABLE
        };
    }

    public static ChannelKind ToKind(string channel)
    {
        return channel switch
        {
            BETA => ChannelKind.Beta,
            ALPHA => ChannelKind.Alpha,
            _ => ChannelKind.Stable
        };
    }
}
