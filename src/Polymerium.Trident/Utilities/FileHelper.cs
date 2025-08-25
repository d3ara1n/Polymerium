using MimeDetective;
using MimeDetective.Definitions;
using Trident.Abstractions.Repositories.Resources;
using FileStream = System.IO.FileStream;

namespace Polymerium.Trident.Utilities
{
    public static class FileHelper
    {
        private static readonly string[] SUPPORTED_BITMAP_MIMES =
        [
            "image/jpeg", "image/png", "image/bmp", "image/gif", "image/tiff"
        ];

        private static readonly IContentInspector inspector =
            new ContentInspectorBuilder { Definitions = DefaultDefinitions.All() }.Build();

        public static string? PickExists(string home, Span<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                var path = Path.Combine(home, candidate);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        public static string? PickRandomly(string home, string pattern)
        {
            if (!Directory.Exists(home))
            {
                return null;
            }

            var files = Directory.GetFiles(home, pattern);
            if (files.Length == 0)
            {
                return null;
            }

            var index = Random.Shared.Next(files.Length);
            return files[index];
        }

        public static bool IsBitmapFile(string path)
        {
            if (File.Exists(path))
            {
                var results = inspector.Inspect(path).ByMimeType();
                if (results.Any(x => SUPPORTED_BITMAP_MIMES.Contains(x.MimeType)))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GuessBitmapExtension(Stream stream, string fallback = "png") =>
            inspector
               .Inspect(stream)
               .ByFileExtension()
               .OrderBy(x => -x.Points)
               .Select(x => x.Extension)
               .FirstOrDefault()
         ?? fallback;

        public static async Task TryWriteToFileAsync(string path, Stream stream)
        {
            var parent = Path.GetDirectoryName(path);
            if (parent != null && !Directory.Exists(parent))
            {
                Directory.CreateDirectory(parent);
            }

            var writer = new FileStream(path, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(writer).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            writer.Close();
        }

        public static string GetAssetFolderName(ResourceKind kind) =>
            kind switch
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
