using System.Text.RegularExpressions;

namespace Trident.Abstractions.Utilities;

public static class LoaderHelper
{
    public const string LOADERID_FORGE = "net.minecraftforge";
    public const string LOADERID_NEOFORGE = "net.neoforged";
    public const string LOADERID_FABRIC = "net.fabricmc";
    public const string LOADERID_QUILT = "org.quiltmc";
    public const string LOADERID_FLINT = "net.flintloader";
    private static readonly Regex PATTERN = new("^(?<identity>[a-z0-9.]+):(?<version>[a-zA-Z0-9_.-]+)$");


    public static string ToDisplayName(string identity) =>
        identity switch
        {
            LOADERID_FORGE => "Forge",
            LOADERID_NEOFORGE => "NeoForge",
            LOADERID_FABRIC => "Fabric",
            LOADERID_QUILT => "Quilt",
            LOADERID_FLINT => "Flint",
            _ => identity
        };

    public static string ToDisplayLabel(string identity, string version) => $"{ToDisplayName(identity)} {version}";

    public static bool TryParse(string lurl, out (string Identity, string Version) result)
    {
        var match = PATTERN.Match(lurl);
        if (match.Success && match.Groups["identity"].Success && match.Groups["version"].Success)
        {
            result.Identity = match.Groups["identity"].Value;
            result.Version = match.Groups["version"].Value;

            return true;
        }

        result = default((string Identity, string Version));
        return false;
    }

    public static string ToLurl(string identity, string version) => $"{identity}:{version}";
}