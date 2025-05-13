using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Engines.Deploying.Stages;
using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Igniters;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Services.Profiles;
using Polymerium.Trident.Utilities;
using Trident.Abstractions;
using Trident.Abstractions.Extensions;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Importers;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Tasks;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Services;

public class InstanceManager(
    ILogger<InstanceManager> logger,
    ProfileManager profileManager,
    RepositoryAgent repositories,
    ImporterAgent importers,
    IServiceProvider provider,
    IHttpClientFactory clientFactory)
{
    // 主要是在 UI 线程中增删改查，实际不需要保证线程同步
    private readonly Dictionary<string, TrackerBase> _trackers = new();
    public event EventHandler<InstallTracker>? InstanceInstalling;
    public event EventHandler<UpdateTracker>? InstanceUpdating;
    public event EventHandler<DeployTracker>? InstanceDeploying;
    public event EventHandler<LaunchTracker>? InstanceLaunching;

    private void TrackerOnCompleted(TrackerBase tracker)
    {
        tracker.Dispose();
        _trackers.Remove(tracker.Key);
    }

    public bool IsTracking(string key, [MaybeNullWhen(false)] out TrackerBase tracker)
    {
        if (_trackers.TryGetValue(key, out var value))
        {
            tracker = value;
            return true;
        }

        tracker = null;
        return false;
    }

    public bool IsInUse(string key) => _trackers.ContainsKey(key);

    private string ComputeWatermark(DeployOptions options)
    {
        var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(options));
        return Convert.ToHexString(SHA1.HashData(data));
    }

    public void DeployAndLaunch(string key, DeployOptions deploy, LaunchOptions launch)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        var path = PathDef.Default.FileOfLockData(key);
        if (deploy.FastMode && File.Exists(path))
        {
            var artifact = JsonSerializer.Deserialize<DataLock>(File.ReadAllText(path), JsonSerializerOptions.Web);

            if (artifact != null
             && artifact.Verify(key, profileManager.GetImmutable(key).Setup, ComputeWatermark(deploy)))
            {
                Launch(key, launch);
                return;
            }
        }

        var tracker = new DeployTracker(key,
                                        async t => await DeployInternalAsync((DeployTracker)t, deploy)
                                                      .ConfigureAwait(false),
                                        t =>
                                        {
                                            TrackerOnCompleted(t);
                                            if (t is { State: TrackerState.Finished })
                                                Launch(key, launch);
                                        });
        _trackers.Add(key, tracker);
        InstanceDeploying?.Invoke(this, tracker);
        tracker.Start();
    }

    #region Common

    private static async Task<MemoryStream> DownloadFileAsync(
        Uri download,
        ulong size,
        Subject<double?>? reporter,
        HttpClient client,
        CancellationToken token)
    {
        var stream = await client.GetStreamAsync(download, token).ConfigureAwait(false);
        var memory = new MemoryStream();
        var buffer = new byte[8 * 1024];
        int read;
        var totalRead = 0L;
        do
        {
            read = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
            await memory.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
            totalRead += read;
            var progress = (double)(totalRead * 100) / size;
            reporter?.OnNext(progress);
        } while (!token.IsCancellationRequested && read > 0);

        stream.Close();

        memory.Position = 0;
        return memory;
    }

    private static async Task ExtractIconFileAsync(string key, ImportedProfileContainer container, HttpClient client)
    {
        var iconReader = await client.GetStreamAsync(container.IconUrl).ConfigureAwait(false);
        var iconMemory = new MemoryStream();
        await iconReader.CopyToAsync(iconMemory).ConfigureAwait(false);
        iconMemory.Position = 0;
        var extension = FileHelper.GuessBitmapExtension(iconMemory);
        var iconPath = PathDef.Default.FileOfIcon(key, extension);
        var dir = Path.GetDirectoryName(iconPath);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        iconMemory.Position = 0;
        var iconWriter = new FileStream(iconPath, FileMode.Create);
        await iconMemory.CopyToAsync(iconWriter).ConfigureAwait(false);
        await iconWriter.FlushAsync().ConfigureAwait(false);
        iconWriter.Close();
        iconMemory.Close();
        iconReader.Close();
    }

    #endregion

    #region Deploy

    public DeployTracker Deploy(string key, DeployOptions options)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        var tracker = new DeployTracker(key,
                                        async t => await DeployInternalAsync((DeployTracker)t, options)
                                                      .ConfigureAwait(false),
                                        TrackerOnCompleted);
        _trackers.Add(key, tracker);
        InstanceDeploying?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task DeployInternalAsync(DeployTracker tracker, DeployOptions options)
    {
        logger.LogInformation("Begin deploy {}", tracker.Key);

        var profile = profileManager.GetImmutable(tracker.Key);
        var engine = new DeployEngine(tracker.Key,
                                      profile.Setup,
                                      provider,
                                      new DeployEngineOptions
                                      {
                                          FastMode = options.FastMode, ResolveDependency = options.ResolveDependency
                                      },
                                      ComputeWatermark(options));

        var watch = Stopwatch.StartNew();
        foreach (var stage in engine)
        {
            if (tracker.Token.IsCancellationRequested)
                break;
            switch (stage)
            {
                case CheckArtifactStage:
                    tracker.StageStream.OnNext(DeployStage.CheckArtifact);
                    tracker.CurrentStage = DeployStage.CheckArtifact;
                    break;
                case InstallVanillaStage:
                    tracker.StageStream.OnNext(DeployStage.InstallVanilla);
                    tracker.CurrentStage = DeployStage.InstallVanilla;
                    break;
                case ProcessLoaderStage:
                    tracker.StageStream.OnNext(DeployStage.ProcessLoader);
                    tracker.CurrentStage = DeployStage.ProcessLoader;
                    break;
                case ResolvePackageStage resolvePackageStage:
                    tracker.StageStream.OnNext(DeployStage.ResolvePackage);
                    tracker.CurrentStage = DeployStage.ResolvePackage;
                    resolvePackageStage
                       .ProgressStream.Subscribe(tracker.ProgressStream)
                       .DisposeWith(resolvePackageStage);
                    break;
                case BuildArtifactStage:
                    tracker.StageStream.OnNext(DeployStage.BuildArtifact);
                    tracker.CurrentStage = DeployStage.BuildArtifact;
                    break;
                case GenerateManifestStage:
                    tracker.StageStream.OnNext(DeployStage.GenerateManifest);
                    tracker.CurrentStage = DeployStage.GenerateManifest;
                    break;
                case SolidifyManifestStage solidifyManifestStage:
                    tracker.StageStream.OnNext(DeployStage.SolidifyManifest);
                    tracker.CurrentStage = DeployStage.SolidifyManifest;
                    solidifyManifestStage
                       .ProgressStream.Subscribe(tracker.ProgressStream)
                       .DisposeWith(solidifyManifestStage);
                    break;
            }

            logger.LogInformation("Enter stage {}", stage.GetType().Name);
            await stage.ProcessAsync(tracker.Token).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        watch.Stop();
        logger.LogInformation("{} deployed in {}ms", tracker.Key, watch.ElapsedMilliseconds);
    }

    #endregion

    #region Launch

    public LaunchTracker Launch(string key, LaunchOptions options)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        var tracker = new LaunchTracker(key,
                                        async t => await LaunchInternalAsync((LaunchTracker)t, options)
                                                      .ConfigureAwait(false),
                                        TrackerOnCompleted);
        _trackers.Add(key, tracker);
        InstanceLaunching?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task LaunchInternalAsync(LaunchTracker tracker, LaunchOptions options)
    {
        logger.LogInformation("Begin launch {}", tracker.Key);

        if (options.Account == null)
            throw new InvalidOperationException("Account is not provided");

        var profile = profileManager.GetImmutable(tracker.Key);

        var artifactPath = PathDef.Default.FileOfLockData(tracker.Key);
        var found = File.Exists(artifactPath);
        if (found)
        {
            var artifact =
                JsonSerializer.Deserialize<DataLock>(await File
                                                          .ReadAllTextAsync(artifactPath, tracker.Token)
                                                          .ConfigureAwait(false),
                                                     JsonSerializerOptions.Web);

            if (artifact == null)
                throw new InvalidOperationException("Artifact is not valid");

            try
            {
                var javaHome = options.JavaHomeLocator(artifact.JavaMajorVersion);
                var workingDir = PathDef.Default.DirectoryOfBuild(tracker.Key);
                var libraryDir = PathDef.Default.CacheLibraryDirectory;
                var assetDir = PathDef.Default.CacheAssetDirectory;
                var nativeDir = PathDef.Default.DirectoryOfNatives(tracker.Key);
                var igniter = artifact.MakeIgniter();
                igniter
                   .AddGameArgument("--width")
                   .AddGameArgument("${resolution_width}")
                   .AddGameArgument("--height")
                   .AddGameArgument("${resolution_height}");
                igniter
                   .SetJavaHome(javaHome)
                   .SetWorkingDirectory(workingDir)
                   .SetAssetRootDirectory(assetDir)
                   .SetNativesRootDirectory(nativeDir)
                   .SetLibraryRootDirectory(libraryDir)
                   .SetClassPathSeparator(';')
                   .SetLauncherName(options.Brand)
                   .SetLauncherVersion(Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Eternal")
                   .SetOsName(PlatformHelper.GetOsName())
                   .SetOsArch(PlatformHelper.GetOsArch())
                   .SetOsVersion(PlatformHelper.GetOsVersion())
                   .SetUserUuid(options.Account.Uuid)
                   .SetUserType(options.Account.UserType)
                   .SetUserName(options.Account.Username)
                   .SetUserAccessToken(options.Account.AccessToken)
                   .SetVersionName(profile.Setup.Version)
                   .SetWindowSize(options.WindowSize)
                   .SetMaxMemory(options.MaxMemory)
                   .SetReleaseType(options.Brand);
                foreach (var additional in options.AdditionalArguments.Split(' '))
                    igniter.AddJvmArgument(additional);


                if (options.Mode == LaunchMode.Debug)
                    igniter.Debug();

                var process = igniter.Build();
                var build = PathDef.Default.DirectoryOfBuild(tracker.Key);
                if (!Directory.Exists(build))
                    Directory.CreateDirectory(build);
                await File
                     .WriteAllLinesAsync(Path.Combine(build, "trident.launch.dump.txt"), process.StartInfo.ArgumentList)
                     .ConfigureAwait(false);
                if (options.Mode == LaunchMode.Managed)
                {
                    var launcher = new LaunchEngine(process);
                    await foreach (var scrap in launcher.WithCancellation(tracker.Token).ConfigureAwait(false))
                        tracker.ScrapStream.OnNext(scrap);
                    tracker.ScrapStream.OnCompleted();

                    if (tracker.Token.IsCancellationRequested)
                    {
                        if (!tracker.IsDetaching)
                            process.Kill();
                    }
                    else
                    {
                        await process.WaitForExitAsync(tracker.Token).ConfigureAwait(false);

                        if (process.ExitCode != 0)
                            throw new Exception($"The process has exited with non-zero code {process.ExitCode}");
                    }

                    process.Close();
                }
                else
                {
                    process.Start();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Launch failed due to exception: {ex}", e.Message);
                throw;
            }
        }
        else
        {
            throw new ArtifactUnavailableException(tracker.Key, artifactPath, found);
        }
    }

    #endregion

    #region Install

    public InstallTracker Install(string key, string label, string? ns, string pid, string? vid)
    {
        // 只有在线安装会有 Tracker，离线导入因为不需要等待，全在前端进行

        var reserved = profileManager.RequestKey(key);
        var tracker = new InstallTracker(reserved.Key,
                                         async t => await InstallInternalAsync((InstallTracker)t,
                                                                               reserved,
                                                                               label,
                                                                               ns,
                                                                               pid,
                                                                               vid)
                                                       .ConfigureAwait(false),
                                         TrackerOnCompleted);
        _trackers.Add(reserved.Key, tracker);
        InstanceInstalling?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task InstallInternalAsync(
        InstallTracker tracker,
        ReservedKey key,
        string label,
        string? ns,
        string pid,
        string? vid)
    {
        logger.LogInformation("Begin install package {} as {}", PackageHelper.ToPurl(label, ns, pid, vid), key.Key);
        var package = await repositories
                           .ResolveAsync(label, ns, pid, vid, Filter.Empty with { Kind = ResourceKind.Modpack })
                           .ConfigureAwait(false);
        var size = package.Size;
        logger.LogDebug("Downloading package file {} sized {} bytes", package.Download.AbsoluteUri, size);
        var client = clientFactory.CreateClient();

        var memory = await DownloadFileAsync(package.Download, size, tracker.ProgressStream, client, tracker.Token)
                        .ConfigureAwait(false);

        logger.LogDebug("Downloaded {} bytes", memory.Length);

        tracker.ProgressStream.OnNext(100d);
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

        tracker.ProgressStream.OnNext(null);
        CompressedProfilePack pack = new(memory) { Reference = package };
        var container = await importers.ImportAsync(pack).ConfigureAwait(false);

        if (container.IconUrl is not null)
            await ExtractIconFileAsync(key.Key, container, client).ConfigureAwait(false);


        logger.LogDebug("{} files collected to extract", container.ImportFileNames.Count);

        await importers.ExtractImportFilesAsync(key.Key, container, pack).ConfigureAwait(false);

        tracker.Source = container.Profile.Setup.Source;

        profileManager.Add(key, container.Profile);

        logger.LogInformation("{} added", key.Key);

        client.Dispose();
    }

    #endregion

    #region Update

    public UpdateTracker Update(string key, string label, string? ns, string pid, string vid)
    {
        if (IsInUse(key))
            throw new InvalidOperationException($"Instance {key} is operated in progress");

        var tracker = new UpdateTracker(key,
                                        async t => await UpdateInternalAsync((UpdateTracker)t, key, label, ns, pid, vid)
                                                      .ConfigureAwait(false),
                                        TrackerOnCompleted);
        _trackers.Add(key, tracker);
        InstanceUpdating?.Invoke(this, tracker);
        tracker.Start();
        return tracker;
    }

    private async Task UpdateInternalAsync(
        UpdateTracker tracker,
        string key,
        string label,
        string? ns,
        string pid,
        string vid)
    {
        logger.LogInformation("Begin update {} from package {}", key, PackageHelper.ToPurl(label, ns, pid, vid));
        var package = await repositories
                           .ResolveAsync(label, ns, pid, vid, Filter.Empty with { Kind = ResourceKind.Modpack })
                           .ConfigureAwait(false);
        var size = package.Size;
        logger.LogDebug("Downloading package file {} sized {} bytes", package.Download.AbsoluteUri, size);
        var client = clientFactory.CreateClient();

        var memory = await DownloadFileAsync(package.Download, size, tracker.ProgressStream, client, tracker.Token)
                        .ConfigureAwait(false);

        logger.LogDebug("Downloaded {} bytes", memory.Length);

        tracker.ProgressStream.OnNext(100d);
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

        tracker.ProgressStream.OnNext(null);
        CompressedProfilePack pack = new(memory) { Reference = package };
        var container = await importers.ImportAsync(pack).ConfigureAwait(false);

        if (container.IconUrl is not null)
            await ExtractIconFileAsync(key, container, client).ConfigureAwait(false);

        logger.LogDebug("{} files collected to extract", container.ImportFileNames.Count);

        // HACK: 因为 Trident 中没有如何滚动活跃的 Import 部分，这里采取直接删除 /build 中的对应文件的方式
        // 这里给出一种未来的方案：/live，类似 /persist 但是赋值自来自 Import，最后软链接进 /build
        // 这样不用管 /build 里的文件内容就能做到 RESET 实例
        // 其实也可以实现上述方案，毕竟用户文件都会被覆盖。整合包缺少配置合并机制，无法实现同时尊重用户配置和整合包配置
        // 不过用户的更改都是对 /build/configs 做出的改变，属于“定制游戏体验”，而玩整合包本就是想要第一方的整合包游戏体验
        // 用户的游戏体验修改被覆盖是意料之内的
        var buildDir = PathDef.Default.DirectoryOfBuild(key);
        var importDir = PathDef.Default.DirectoryOfImport(key);

        if (Directory.Exists(buildDir) && Directory.Exists(importDir))
        {
            var queue = new Queue<string>();
            var cleans = new List<string>();
            queue.Enqueue(importDir);
            while (queue.TryDequeue(out var dir))
            {
                var files = Directory.GetFiles(dir);
                var dirs = Directory.GetDirectories(dir);
                foreach (var sub in dirs)
                {
                    queue.Enqueue(sub);
                    cleans.Add(sub);
                }

                foreach (var file in files)
                {
                    var relative = Path.GetRelativePath(importDir, file);
                    var target = Path.Combine(buildDir, relative);
                    if (File.Exists(target))
                        File.Delete(target);
                }
            }

            // 这里的排序是为了遍历顺序永远是级别深入的在前，以此代替 DFS 达到效果
            // 证明有限遍历到 A 的子文件夹 A/B，由 A/B(3) 长度必定大于 A(1)
            foreach (var target in cleans
                                  .OrderByDescending(x => x.Length)
                                  .Select(x => Path.GetRelativePath(importDir, x))
                                  .Select(x => Path.Combine(buildDir, x))
                                  .Where(Directory.Exists)
                                  .Where(x => Directory.GetDirectories(x).Length == 0
                                           && Directory.GetFiles(x).Length == 0))
                Directory.Delete(target);
        }


        if (Directory.Exists(importDir))
            Directory.Delete(importDir, true);

        await importers.ExtractImportFilesAsync(key, container, pack).ConfigureAwait(false);

        tracker.OldSource = profileManager.GetImmutable(key).Setup.Source;
        tracker.NewSource = container.Profile.Setup.Source;

        profileManager.Update(key,
                              container.Profile.Setup.Source,
                              container.Profile.Name,
                              container.Profile.Setup.Version,
                              container.Profile.Setup.Loader,
                              container.Profile.Setup.Packages.Select(x => x.Purl).ToList(),
                              container.Profile.Overrides);

        logger.LogInformation("{} updated", key);

        client.Dispose();
    }

    #endregion
}