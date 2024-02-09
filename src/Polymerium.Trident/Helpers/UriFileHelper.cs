namespace Polymerium.Trident.Helpers;

public static class UriFileHelper
{
    public static async Task SaveAsync(Uri source, string target, IHttpClientFactory factory,
        CancellationToken token = default)
    {
        string[] web = ["http", "https"];
        var dir = Path.GetDirectoryName(target);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        if (source.IsFile && File.Exists(source.AbsolutePath))
        {
            File.Copy(source.AbsolutePath, target);
        }
        else if (web.Any(x => source.Scheme.Equals(x, StringComparison.InvariantCultureIgnoreCase)))
        {
            using var client = factory.CreateClient();
            await using var writer = new FileStream(target, FileMode.Create, FileAccess.Write);
            var reader = await client.GetStreamAsync(source, token);
            await reader.CopyToAsync(writer, token);
        }
        else
        {
            throw new NotSupportedException("Unsupported url or source file not found");
        }
    }
}