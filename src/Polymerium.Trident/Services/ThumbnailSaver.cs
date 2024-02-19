using Polymerium.Trident.Helpers;

namespace Polymerium.Trident.Services
{
    public class ThumbnailSaver(TridentContext context, IHttpClientFactory factory)
    {
        public async Task SaveAsync(string key, Uri url, CancellationToken token = default)
        {
            string target = GetTargetFileName(key);
            await UriFileHelper.SaveAsync(url, target, factory, token);
        }

        public async Task SaveAsync(string key, Stream stream, CancellationToken token = default)
        {
            string target = GetTargetFileName(key);
            await using FileStream writer = File.OpenWrite(target);
            await stream.CopyToAsync(writer, token);
        }

        private string GetTargetFileName(string key)
        {
            return Path.Combine(context.ThumbnailDir, $"{key}.png");
        }

        public string? Get(string key)
        {
            string path = Path.Combine(context.ThumbnailDir, $"{key}.png");
            if (File.Exists(path))
            {
                return path;
            }

            return null;
        }
    }
}