using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.Core.Engines.Downloading;
using Polymerium.Core.Engines.Restoring;
using Polymerium.Core.Models.Mojang;
using Polymerium.Core.Models.Mojang.Converters;
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
        public RestoreEngine(ILogger<RestoreEngine> logger, GameManager manager, ResolveEngine resolver, DownloadEngine downloader, IFileBaseService fileBaseService)
        {
            _logger = logger;
            _manager = manager;
            _resolver = resolver;
            _downloader = downloader;
            _fileBaseService = fileBaseService;
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ArgumentsItemConverter());
            settings.Converters.Add(new RuleFeaturesConverter());
            settings.Converters.Add(new AssetsIndexConverter());
            _wapooOptions = new WapooOptions()
            {
                JsonSerializerOptions = settings
            };
        }

        public async Task<bool> ResotreAsync(GameInstance instance, RestoreProgressHandler callback = null) => await RestoreAsync(instance, callback, CancellationToken.None);

        public async Task<bool> RestoreAsync(GameInstance instance, RestoreProgressHandler callback, CancellationToken token)
        {
            _logger.LogInformation("Restore begin");
            var index = await EnsureInstanceIndexCreatedAsync(instance, callback, token);
            if (index == null) return false;
            // 下载 jar
            var jarFile = await EnsureJarDownloadedAsync(instance, index.Value, callback, token);
            if (jarFile == null) return false;
            // 补全 assets
            var assetIndex = await EnsureAssetsIndexCreatedAsync(instance, index.Value, callback, token);
            if (assetIndex == null) return false;
            if (!await EnsureAssetsCompletedAsync(instance, assetIndex.Value, callback, token)) return false;
            callback?.Invoke(this, RestoreProgressEventArgs.CreateComplete());
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
                var path = new Uri($"poly-file:///assets/objects/{item[..2]}/{item}", UriKind.Absolute);
                if (!await _fileBaseService.VerfyHashAsync(path, item, sha1))
                {
                    group.Add($"https://resources.download.minecraft.net/{item[..2]}/{item}", _fileBaseService.Locate(path));
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
                callback?.Invoke(this, RestoreProgressEventArgs.CreateUpdate(RestoreProgressType.Core, "client.jar"));
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

        private async Task<Models.Mojang.Index?> EnsureInstanceIndexCreatedAsync(GameInstance instance, RestoreProgressHandler callback, CancellationToken token)
        {
            if (token.IsCancellationRequested) return null;
            var indexFilePath = new Uri($"poly-file://{instance.Id}/index.json", UriKind.Absolute);
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
