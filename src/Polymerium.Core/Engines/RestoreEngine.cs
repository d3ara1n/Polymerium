using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Core.Engines.Downloading;
using Polymerium.Core.Engines.Restoring;
using Polymerium.Core.Models.Mojang;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Wupoo;

namespace Polymerium.Core.Engines
{
    public delegate void RestoreProgressHandler(RestoreEngine sender, RestoreProgressEventArgs args);

    public class RestoreEngine
    {
        private readonly ILogger _logger;
        private readonly GameManager _manager;
        private readonly ResolveEngine _resolver;
        private readonly DownloadEngine _downloader;
        private readonly WapooOptions _wapooOptions;
        private readonly IFileBaseService _fileBaseService;
        private readonly SHA1 _sha1 = SHA1.Create();

        public RestoreEngine(ILogger<RestoreEngine> logger, GameManager manager, ResolveEngine resolver, DownloadEngine downloader, IFileBaseService fileBaseService)
        {
            _logger = logger;
            _manager = manager;
            _resolver = resolver;
            _downloader = downloader;
            _fileBaseService = fileBaseService;
            var settings = new JsonSerializerSettings();
            _wapooOptions = new WapooOptions()
            {
                JsonSerializerOptions = settings
            };
        }

        public async Task<bool> RestoreAsync(GameInstance instance, RestoreProgressHandler callback = null) => await RestoreAsync(instance, callback, CancellationToken.None);

        public async Task<bool> RestoreAsync(GameInstance instance, RestoreProgressHandler callback, CancellationToken token)
        {
            _logger.LogInformation("Restore begin");
            var res = RestoreInternalAsync(instance, callback, token);
            res.Wait();
            if (res.IsCompletedSuccessfully)
            {
                _logger.LogInformation("Restore finished with {} error for {}", res.Result ? "no" : "an", instance.Id);
                callback?.Invoke(this, RestoreProgressEventArgs.CreateComplete());
            }
            else if (res.IsCanceled)
            {
                _logger.LogInformation("Restore canceled for {}", instance.Id);
                callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.Canceled, null));
            }
            else
            {
                if (res.Exception != null)
                {
                    _logger.LogError(res.Exception, "Restore ran into excepiton for {}", instance.Id);
                    callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.ExceptionOccurred, null, res.Exception));
                }
                else
                {
                    _logger.LogError("Restore ran into unknown exception for {}", instance.Id);
                    callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.Unknown, null));
                }
            }
            return res.Result;
        }

        private async Task<bool> RestoreInternalAsync(GameInstance instance, RestoreProgressHandler callback, CancellationToken token)
        {
            var index = await EnsureIndexCreatedAsync(instance, callback, token);
            if (index == null) return false;
            // 下载 jar
            var jarFile = await EnsureJarDownloadedAsync(instance, index.Value, callback, token);
            if (jarFile == null) return false;
            // 补全 assets
            var assetIndex = await EnsureAssetsIndexCreatedAsync(instance, index.Value, callback, token);
            if (assetIndex == null) return false;
            if (!await EnsureAssetsCompletedAsync(instance, assetIndex.Value, callback, token)) return false;
            if (!await EnsureLibrariesCompletedAsync(instance, index.Value, callback, token)) return false;
            return true;
        }

        private async Task<bool> UnzipFileAsync(string from, string to, RestoreProgressHandler callback, CancellationToken token)
        {
            try
            {
                ZipFile.ExtractToDirectory(from, to, true);
                return true;
            }
            catch (Exception e)
            {
                callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.ExceptionOccurred, Path.GetFileName(from), e));
                return false;
            }
        }

        private async Task<bool> EnsureLibrariesCompletedAsync(GameInstance instance, Models.Mojang.Index index, RestoreProgressHandler callback, CancellationToken token)
        {
            if (token.IsCancellationRequested) return false;
            callback?.Invoke(this, RestoreProgressEventArgs.CreateUpdate(RestoreProgressType.Libraries, null));
            var libDir = new Uri("poly-file:///libraries/");
            var nativesDir = new Uri($"poly-file://{instance.Id}/natives/");
            var sha1 = SHA1.Create();
            var group = new DownloadTaskGroup() { Token = token };
            _fileBaseService.RemoveDirectory(nativesDir);
            foreach (var item in index.Libraries.Where(x => x.Verfy()))
            {
                if (token.IsCancellationRequested) return false;
                var libPath = new Uri(libDir, item.Downloads.Artifact.Path);
                if (!await _fileBaseService.VerfyHashAsync(libPath, item.Downloads.Artifact.Sha1, sha1))
                {
                    group.TryAdd(item.Downloads.Artifact.Url.AbsoluteUri, _fileBaseService.Locate(libPath), out var _);
                }
                if (item.Natives.HasValue)
                {
                    // support more platform not only windows
                    var native = item.Natives.Value.Windows;
                    if (native == "unknown")
                    {
                        callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.OsNotSupport, item.Name));
                        return false;
                    }
                    var classifier = item.Downloads.Classifiers.FirstOrDefault(x => x.Identity == native);
                    var path = new Uri(libDir, classifier.Path);
                    if (!await _fileBaseService.VerfyHashAsync(path, classifier.Sha1, sha1))
                    {
                        if (group.TryAdd(classifier.Url.AbsoluteUri, _fileBaseService.Locate(path), out var task))
                        {
                            task.CompletedCallback = async (t, s) =>
                            {
                                if (s) await UnzipFileAsync(t.Destination, _fileBaseService.Locate(nativesDir), callback, token);
                            };
                        }
                    }
                    else
                    {
                        await UnzipFileAsync(_fileBaseService.Locate(path), _fileBaseService.Locate(nativesDir), callback, token);
                    }
                }
            }
            if (group.TotalCount == 0)
            {
                return true;
            }
            group.CompletedDelegate = (g, t, c, s) =>
            {
                callback?.Invoke(this, RestoreProgressEventArgs.CreateDownload(Path.GetFileName(t.Destination), c, g.TotalCount));
            };
            _downloader.Enqueue(group);
            group.Wait();
            return true;
        }

        private async Task<bool> EnsureAssetsCompletedAsync(GameInstance instance, AssetsIndex index, RestoreProgressHandler callback, CancellationToken token)
        {
            if (token.IsCancellationRequested) return false;
            callback?.Invoke(this, RestoreProgressEventArgs.CreateUpdate(RestoreProgressType.Assets, null));
            var sha1 = SHA1.Create();
            var group = new DownloadTaskGroup()
            {
                Token = token
            };
            foreach (var item in index.Objects.Select(x => x.Hash))
            {
                if (token.IsCancellationRequested) return false;
                var path = new Uri($"poly-file:///assets/objects/{item[..2]}/{item}", UriKind.Absolute);
                if (!await _fileBaseService.VerfyHashAsync(path, item, sha1))
                {
                    group.TryAdd($"https://resources.download.minecraft.net/{item[..2]}/{item}", _fileBaseService.Locate(path), out var _);
                }
            }
            if (group.TotalCount == 0)
            {
                return true;
            }
            group.CompletedDelegate = (g, t, d, success) =>
            {
                callback?.Invoke(this, RestoreProgressEventArgs.CreateDownload(Path.GetFileName(t.Destination), d, g.TotalCount));
            };
            _downloader.Enqueue(group);
            group.Wait();
            return true;
        }

        private async Task<Uri> EnsureJarDownloadedAsync(GameInstance instance, Models.Mojang.Index index, RestoreProgressHandler callback, CancellationToken token)
        {
            if (token.IsCancellationRequested) return null;
            var jarFilePath = new Uri($"poly-file://{instance.Id}/client.jar", UriKind.Absolute);
            if (!await _fileBaseService.VerfyHashAsync(jarFilePath, index.Downloads.Client.Sha1, SHA1.Create()))
            {
                callback?.Invoke(this, RestoreProgressEventArgs.CreateUpdate(RestoreProgressType.Core, $"{instance.FolderName}.jar"));
                var task = new DownloadTask()
                {
                    Token = token,
                    Source = index.Downloads.Client.Url.AbsoluteUri,
                    Destination = _fileBaseService.Locate(jarFilePath)
                };
                _downloader.Enqueue(task);
                task.Wait();
            }
            return jarFilePath;
        }

        private async Task<AssetsIndex?> EnsureAssetsIndexCreatedAsync(GameInstance instance, Models.Mojang.Index index, RestoreProgressHandler callback, CancellationToken token)
        {
            if (token.IsCancellationRequested) return null;
            var indexFilePath = new Uri($"poly-file:///assets/indexes/{Path.GetFileName(index.AssetIndex.Url.AbsoluteUri)}", UriKind.Absolute);
            string content = null;
            if (!await _fileBaseService.VerfyHashAsync(indexFilePath, index.AssetIndex.Sha1, SHA1.Create()) || !_fileBaseService.TryReadAllText(indexFilePath, out content))
            {
                Exception exception = null;
                await Wapoo.Wohoo(index.AssetIndex.Url.AbsoluteUri, _wapooOptions)
                    .ForAnyResult(async (_, stream) =>
                    {
                        using (var reader = new StreamReader(stream))
                            content = await reader.ReadToEndAsync();
                    })
                    .WhenException<Exception>(e => exception = e)
                    .FetchAsync();
                if (content == null)
                {
                    callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.ResourceNotReacheable, "assets.json", exception));
                    return null;
                }
                _fileBaseService.WriteAllText(indexFilePath, content);
            }
            try
            {
                var assets = JsonConvert.DeserializeObject<AssetsIndex>(content, _wapooOptions.JsonSerializerOptions);
                return assets;
            }
            catch (Exception e)
            {
                callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.SerializationFailure, $"assets.json", e));
                return null;
            }
        }

        private async Task<Models.Mojang.Index?> EnsureIndexCreatedAsync(GameInstance instance, RestoreProgressHandler callback, CancellationToken token)
        {
            if (token.IsCancellationRequested) return null;
            var indexFilePath = new Uri($"poly-file:///local/indexes/{instance.Metadata.CoreVersion}.json", UriKind.Absolute);
            string content = null;
            var meta = instance.Metadata;
            if (!_fileBaseService.TryReadAllText(indexFilePath, out content))
            {
                VersionManifest? manifest = null;
                Exception exception = null;
                await Wapoo.Wohoo("https://piston-meta.mojang.com/mc/game/version_manifest.json", _wapooOptions)
                    .ForJsonResult<VersionManifest>(it => manifest = it)
                    .WhenException<Exception>(e => exception = e)
                    .FetchAsync();
                if (manifest == null)
                {
                    callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.ResourceNotReacheable, "version_manifest.json", exception));
                    return null;
                }
                var version = manifest?.Versions.FirstOrDefault(x => x.Id == meta.CoreVersion);
                if (version == null)
                {
                    callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.ResourceNotFound, $"{meta.CoreVersion}.json"));
                    return null;
                }
                if (token.IsCancellationRequested) return null;
                await Wapoo.Wohoo(version?.Url.AbsoluteUri, _wapooOptions)
                    .ForAnyResult(async (_, stream) =>
                    {
                        using (var reader = new StreamReader(stream))
                            content = await reader.ReadToEndAsync();
                    })
                    .WhenException<Exception>(e => exception = e)
                    .FetchAsync();
                if (content == null)
                {
                    callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.ResourceNotReacheable, $"{meta.CoreVersion}.json", exception));
                    return null;
                }
                _fileBaseService.WriteAllText(indexFilePath, content);
            }
            try
            {
                var index = JsonConvert.DeserializeObject<Models.Mojang.Index>(content, _wapooOptions.JsonSerializerOptions);
                return index;
            }
            catch (Exception e)
            {
                callback?.Invoke(this, RestoreProgressEventArgs.CreateError(RestoreError.SerializationFailure, $"{meta.CoreVersion}.json", e));
                return null;
            }
        }
    }
}