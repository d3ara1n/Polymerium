using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines.Downloading;
using System.Diagnostics;
using System.IO.Compression;
using Trident.Abstractions.Exceptions;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class SolidifyTransientStage(DownloadEngine downloader) : StageBase
{
    private Action<string, uint, uint>? callback;

    protected override async Task OnProcessAsync()
    {
        var transient = Context.Transient!;

        foreach (var fragile in transient.FragileFiles)
            downloader.AddTask(new DownloadTask(fragile.SourcePath, fragile.Url, fragile.Sha1, fragile));

        foreach (var present in transient.PresentFiles)
            downloader.AddTask(new DownloadTask(present.SourcePath, present.Url, present.Sha1, present));

        Logger.LogInformation("Created download tasks of {count}", downloader.Count);

        var count = 0u;
        var entities = new List<Entity>();

        var watch = Stopwatch.StartNew();

        var cancel = CancellationTokenSource.CreateLinkedTokenSource(Context.Token);

        await foreach (var done in downloader.WithCancellation(cancel.Token).ConfigureAwait(false))
        {
            if (done.State == DownloadResult.DownloadResultState.Broken)
            {
                cancel.Cancel();
                throw new BadNetworkException($"Cannot download to {done.Target} from {done.Source.AbsoluteUri}");
            }

            switch (done.Tag)
            {
                case TransientData.FragileFile fragile:
                    entities.Add(new Entity(fragile.TargetPath, fragile.SourcePath));
                    break;

                case TransientData.PersistentFile:
                    throw new NotImplementedException();
                case TransientData.PresentFile:
                    // do nothing
                    break;

                default:
                    throw new NotImplementedException();
            }

            callback?.Invoke(done.Target, ++count, done.Total);
        }

        watch.Stop();

        Logger.LogInformation("Download finished in {ms}ms", watch.ElapsedMilliseconds);

        foreach (var presistent in transient.PersistentFiles)
            if (!File.Exists(presistent.TargetPath))
            {
                var dir = Path.GetDirectoryName(presistent.TargetPath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.Copy(presistent.SourcePath, presistent.TargetPath);
            }

        foreach (var explosive in transient.ExplosiveFiles)
        {
            if (!Directory.Exists(explosive.TargetDirectory)) Directory.CreateDirectory(explosive.TargetDirectory);
            ZipFile.ExtractToDirectory(explosive.SourcePath, explosive.TargetDirectory, true);
        }

        Snapshot.Populate(Context.Context.InstanceHomePath(Context.Key), entities);

        Context.IsSolidified = true;
    }

    public void SetHandler(Action<string, uint, uint> handler)
    {
        callback = handler;
    }
}