using System.Diagnostics;
using System.IO.Compression;
using System.Reactive.Subjects;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Trident.Abstractions;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class SolidifyManifestStage(ILogger<SolidifyManifestStage> logger, IHttpClientFactory factory) : StageBase
{
    public Subject<(int, int)> ProgressStream { get; } = new();

    protected override async Task OnProcessAsync(CancellationToken token)
    {
        var manifest = Context.Manifest!;

        var files = new List<object>();

        foreach (var fragile in manifest.FragileFiles)
            files.Add(fragile);

        foreach (var present in manifest.PresentFiles)
            files.Add(present);

        foreach (var persistent in manifest.PersistentFiles)
            files.Add(persistent);

        logger.LogInformation("Created solidifying tasks of {}", files.Count + manifest.ExplosiveFiles.Count);

        var buildDir = PathDef.Default.DirectoryOfBuild(Context.Key);
        var fullBuildDir = Path.GetFullPath(buildDir);

        var downloaded = 0;
        var semaphore = new SemaphoreSlim(Math.Max(Environment.ProcessorCount - 1, 1));
        var watch = Stopwatch.StartNew();
        var cancel = CancellationTokenSource.CreateLinkedTokenSource(token);
        var entities = new List<Snapshot.Entity>();

        ProgressStream.OnNext((downloaded, files.Count));
        var tasks = files
                   .Select(async x =>
                    {
                        if (cancel.IsCancellationRequested)
                            return;
                        try
                        {
                            await semaphore.WaitAsync(cancel.Token).ConfigureAwait(false);
                            switch (x)
                            {
                                case EntityManifest.FragileFile fragile:
                                {
                                    if (!Verify(fragile.SourcePath, fragile.Hash))
                                    {
                                        logger.LogDebug("Starting download fragile file {} from {}",
                                                        fragile.SourcePath,
                                                        fragile.Url);
                                        var dir = Path.GetDirectoryName(fragile.SourcePath);
                                        if (dir != null && !Directory.Exists(dir))
                                            Directory.CreateDirectory(dir);
                                        var client = factory.CreateClient();
                                        var reader = await client
                                                          .GetStreamAsync(fragile.Url, cancel.Token)
                                                          .ConfigureAwait(false);
                                        var writer = new FileStream(fragile.SourcePath,
                                                                    FileMode.Create,
                                                                    FileAccess.Write);
                                        await reader.CopyToAsync(writer, cancel.Token).ConfigureAwait(false);
                                        reader.Close();
                                        writer.Close();
                                    }
                                    else
                                    {
                                        logger.LogDebug("Skipped download fragile file {} from {}",
                                                        fragile.SourcePath,
                                                        fragile.Url);
                                    }

                                    entities.Add(new Snapshot.Entity(fragile.TargetPath, fragile.SourcePath));

                                    break;
                                }
                                case EntityManifest.PresentFile present:
                                {
                                    if (!Verify(present.Path, present.Hash))
                                    {
                                        var dir = Path.GetDirectoryName(present.Path);
                                        if (dir != null && !Directory.Exists(dir))
                                            Directory.CreateDirectory(dir);
                                        var client = factory.CreateClient();
                                        var reader = await client
                                                          .GetStreamAsync(present.Url, cancel.Token)
                                                          .ConfigureAwait(false);
                                        var writer = new FileStream(present.Path, FileMode.Create, FileAccess.Write);
                                        await reader.CopyToAsync(writer, cancel.Token).ConfigureAwait(false);
                                        reader.Close();
                                        writer.Close();
                                    }
                                    else
                                    {
                                        logger.LogDebug("Skipped download present file {} from {}",
                                                        present.Path,
                                                        present.Url);
                                    }

                                    break;
                                }
                                case EntityManifest.PersistentFile persistent:
                                {
                                    if (!File.Exists(persistent.TargetPath))
                                    {
                                        var dir = Path.GetDirectoryName(persistent.TargetPath);
                                        if (dir != null && !Directory.Exists(dir))
                                            Directory.CreateDirectory(dir);

                                        if (persistent.IsPhantom)
                                        {
                                            logger.LogDebug("Linking persistent file from {} to {}",
                                                            persistent.SourcePath,
                                                            persistent.TargetPath);
                                            entities.Add(new Snapshot.Entity(persistent.TargetPath,
                                                                             persistent.SourcePath));
                                        }
                                        else
                                        {
                                            logger.LogDebug("Copying persistent file from {} to {}",
                                                            persistent.SourcePath,
                                                            persistent.TargetPath);
                                            File.Copy(persistent.SourcePath, persistent.TargetPath);
                                        }
                                    }
                                    else
                                    {
                                        logger.LogDebug("Skipped persistent file {}", persistent.SourcePath);
                                    }

                                    break;
                                }
                            }

                            Interlocked.Increment(ref downloaded);
                            ProgressStream.OnNext((downloaded, files.Count + manifest.ExplosiveFiles.Count));
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            await cancel.CancelAsync().ConfigureAwait(false);
                            logger.LogError(ex, "Failed to solidify {}", x);
                            throw;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    })
                   .ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
        foreach (var explosive in manifest.ExplosiveFiles)
        {
            if (Directory.Exists(explosive.TargetDirectory) && explosive.IsDestructive)
            {
                // 只有 build 目录里才具有摧毁性
                var full = Path.GetFullPath(explosive.TargetDirectory);
                if (full.StartsWith(fullBuildDir))
                {
                    logger.LogDebug("Destroying {}", full);
                    Directory.Delete(explosive.TargetDirectory, true);
                }
            }

            if (!Directory.Exists(explosive.TargetDirectory))
                Directory.CreateDirectory(explosive.TargetDirectory);

            logger.LogDebug("Extracting {} to {}", explosive.SourcePath, explosive.TargetDirectory);
            ZipFile.ExtractToDirectory(explosive.SourcePath, explosive.TargetDirectory, true);
            ProgressStream.OnNext((++downloaded, files.Count + manifest.ExplosiveFiles.Count));
        }


        watch.Stop();
        logger.LogInformation("Solidifying finished in {ms}ms", watch.ElapsedMilliseconds);

        Snapshot.Apply(buildDir, entities);

        Context.IsSolidified = true;
    }

    private bool Verify(string path, string? hash)
    {
        if (File.Exists(path))
        {
            if (hash != null)
            {
                var reader = File.OpenRead(path);
                var computed = Convert.ToHexString(SHA1.HashData(reader));
                reader.Dispose();
                return hash.Equals(computed, StringComparison.InvariantCultureIgnoreCase);
            }

            return true;
        }

        return false;
    }

    public override void Dispose()
    {
        base.Dispose();
        ProgressStream.Dispose();
    }
}