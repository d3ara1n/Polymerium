using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Helpers
{
    public static class FileNameHelper
    {
        public static string Sanitize(string fileName)
        {
            string output = fileName.Replace(' ', '_');
            foreach (char ch in Path.GetInvalidFileNameChars())
            {
                output = output.Replace(ch, '_');
            }

            while (output.Contains("__"))
            {
                output = output.Replace("__", "_");
            }

            return output;
        }

        public static string GetAssetFolderName(AssetKind kind)
        {
            return kind switch
            {
                AssetKind.Mod => "mods",
                AssetKind.Save => "saves",
                AssetKind.Screenshot => "screenshots",
                AssetKind.ShaderPack => "shaderpacks",
                AssetKind.ResourcePack => "resourcepacks",
                AssetKind.DataPack => "datapacks",
                _ => throw new NotImplementedException()
            };
        }

        public static string GetAssetFolderName(ResourceKind kind)
        {
            return kind switch
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
}