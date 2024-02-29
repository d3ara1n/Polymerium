using System.Diagnostics;

namespace Polymerium.Trident.Helpers
{
    public static class UriFileHelper
    {
        public static async Task SaveAsync(Uri source, string target, IHttpClientFactory factory,
            CancellationToken token = default)
        {
            string[] web = ["http", "https"];
            string? dir = Path.GetDirectoryName(target);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (source.IsFile && File.Exists(source.AbsolutePath))
            {
                File.Copy(source.AbsolutePath, target);
            }
            else if (web.Any(x => source.Scheme.Equals(x, StringComparison.InvariantCultureIgnoreCase)))
            {
                using HttpClient client = factory.CreateClient();
                await using FileStream writer = new(target, FileMode.Create, FileAccess.Write);
                Stream reader = await client.GetStreamAsync(source, token);
                await reader.CopyToAsync(writer, token);
            }
            else
            {
                throw new NotSupportedException("Unsupported url or source file not found");
            }
        }

        public static bool OpenInExternal(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}