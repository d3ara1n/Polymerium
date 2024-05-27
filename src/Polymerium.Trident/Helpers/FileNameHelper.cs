using System.Text;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Helpers
{
    public static class FileNameHelper
    {
        public static string Sanitize(string fileName)
        {
            var sb = new StringBuilder(fileName.Length);
            var invalids = Path.GetInvalidFileNameChars();
            foreach (var ch in fileName)
            {
                if ((ch == ' ' || invalids.Contains(ch)) && (sb.Length > 0 && sb[^1] != '_'))
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        public static string GetAssetFolderName(AssetKind kind) => kind switch
        {
            AssetKind.Mod => "mods",
            AssetKind.Save => "saves",
            AssetKind.Screenshot => "screenshots",
            AssetKind.ShaderPack => "shaderpacks",
            AssetKind.ResourcePack => "resourcepacks",
            AssetKind.DataPack => "datapacks",
            AssetKind.Log => "logs",
            _ => throw new NotImplementedException()
        };

        public static string GetAssetFolderName(ResourceKind kind) => kind switch
        {
            ResourceKind.Mod => "mods",
            ResourceKind.World => "saves",
            ResourceKind.ShaderPack => "shaderpacks",
            ResourceKind.ResourcePack => "resourcepacks",
            ResourceKind.DataPack => "datapacks",
            _ => throw new NotImplementedException()
        };
    }
}