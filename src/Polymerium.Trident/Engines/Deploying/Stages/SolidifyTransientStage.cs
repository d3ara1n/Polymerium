using System.Security.Cryptography;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class SolidifyTransientStage(IHttpClientFactory factory) : StageBase
{
    private Action<string, uint, uint>? callback;

    protected override async Task OnProcessAsync()
    {
        var transient = Context.Transient!;
        var total =
            (uint)(transient.FragileFiles.Count + transient.PersistentFiles.Count + transient.PresentFiles.Count);
        var count = 0u;

        using var client = factory.CreateClient();

        foreach (var fragile in transient.FragileFiles)
        {
            await DownloadAsync(client, fragile.SourcePath, fragile.Url, fragile.Sha1);
            if (!File.Exists(fragile.TargetPath))
            {
                var dir = Path.GetDirectoryName(fragile.TargetPath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.CreateSymbolicLink(fragile.TargetPath, fragile.SourcePath);
            }

            callback?.Invoke(fragile.TargetPath, ++count, total);
        }

        foreach (var present in transient.PresentFiles)
        {
            await DownloadAsync(client, present.SourcePath, present.Url, present.Sha1);
            callback?.Invoke(present.SourcePath, ++count, total);
        }

        foreach (var persistent in transient.PersistentFiles)
        {
            if (!File.Exists(persistent.TargetPath))
            {
                var dir = Path.GetDirectoryName(persistent.TargetPath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.Copy(persistent.SourcePath, persistent.TargetPath);
            }

            callback?.Invoke(persistent.TargetPath, ++count, total);
        }

        Context.IsSolidified = true;
    }

    private async Task DownloadAsync(HttpClient client, string path, Uri url, string? hash)
    {
        if (File.Exists(path))
        {
            if (hash == null) return;
            var reader = File.OpenRead(path);
            var equal = BitConverter.ToString(await SHA1.HashDataAsync(reader)).Replace("-", string.Empty)
                .Equals(hash, StringComparison.CurrentCultureIgnoreCase);
            await reader.DisposeAsync();
            if (equal) return;
        }

        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        await using var stream = await client.GetStreamAsync(url);
        await using var writer = File.Create(path);
        await stream.CopyToAsync(writer);
    }

    public void SetHandler(Action<string, uint, uint> handler)
    {
        callback = handler;
    }
}