using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MirrorChyan.Net.Models;
using MirrorChyan.Net.Services;
using NuGet.Versioning;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;
using VelopackExtension.MirrorChyan.Exceptions;

namespace VelopackExtension.MirrorChyan.Sources;

/// <summary>
/// MirrorChyan 更新源，实现 Velopack 的 IUpdateSource 接口
///
/// Remark: 由于 Velopack 的增量包要求每个版本都有增量+最新版本有全量文件，而 MirrorChyan 无法实现最新版本没有获取相邻版本之间的增量版本集合能力，因此无法实现增量更新。
/// </summary>
public class MirrorChyanSource(
    IMirrorChyanService mirrorChyanService,
    IOptions<MirrorChyanSourceOptions> sourceOptions,
    IOptions<MirrorChyanOptions> mirrorChyanOptions,
    IHttpClientFactory httpClientFactory) : IUpdateSource
{
    // 缓存下载 URL，因为 VelopackAsset 没有自定义属性存储 URL
    // Velopack 的设计限制导致需要这个中间变量
    private readonly ConcurrentDictionary<string, VersionModel> _downloadUrls = new();

    /// <inheritdoc />
    public async Task<VelopackAssetFeed> GetReleaseFeed(
        IVelopackLogger logger,
        string? appId,
        string channel,
        Guid? stagingId = null,
        VelopackAsset? latestLocalRelease = null)
    {
        var currentSourceOptions = sourceOptions.Value;
        var currentMirrorChyanOptions = mirrorChyanOptions.Value;

        try
        {
            var version =
                await mirrorChyanService.GetLatestVersionAsync(currentSourceOptions.Cdk, currentSourceOptions.Channel);

            if (version.Artifact is null)
                throw new ResourceNotDownloadableException("Resource is not available. Maybe cdk is not provided.");

            // 文件名 包名+版本+os+arch.nupkg 拼接
            var packageId = appId ?? currentMirrorChyanOptions.ProductId;
            var fileName = BuildFileName(packageId,
                                         version.Version,
                                         version.Artifact.Os,
                                         version.Artifact.Arch,
                                         version.Artifact.Kind);

            // 转换为 VelopackAsset
            var asset = new VelopackAsset
            {
                PackageId = packageId,
                Version =
                    SemanticVersion.Parse(version.Version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                                              ? version.Version[1..]
                                              : version.Version),
                Type = version.Artifact.Kind == UpdateKind.Full ? VelopackAssetType.Full : VelopackAssetType.Delta,
                FileName = fileName,
                SHA256 = version.Artifact.Sha256,
                Size = version.Artifact.FileSize,
                NotesMarkdown = version.ReleaseNote
            };

            // 缓存下载 URL 以供 DownloadReleaseEntry 使用
            _downloadUrls[fileName] = version;

            return new() { Assets = [asset] };
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to get release feed from MirrorChyan: {ex.Message}");
            return new() { Assets = [] };
        }
    }

    /// <inheritdoc />
    public async Task DownloadReleaseEntry(
        IVelopackLogger logger,
        VelopackAsset releaseEntry,
        string localFile,
        Action<int> progress,
        CancellationToken cancelToken = default)
    {
        // 必须先 GetReleaseFeed 缓存 MirrorChyan 资源才能进行下一步
        if (!_downloadUrls.TryGetValue(releaseEntry.FileName, out var version) || version.Artifact is null)
        {
            throw new KeyNotFoundException($"No cached asset found for {releaseEntry.Version}");
        }

        logger.Info($"Downloading {releaseEntry.FileName} from MirrorChyan: {version.Artifact.Url}");

        var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync(version.Artifact.Url,
                                                       HttpCompletionOption.ResponseHeadersRead,
                                                       cancelToken);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? version.Artifact.FileSize;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancelToken);
        await using var fileStream = new FileStream(localFile,
                                                    FileMode.Create,
                                                    FileAccess.Write,
                                                    FileShare.None,
                                                    bufferSize: 81920,
                                                    useAsync: true);

        var buffer = new byte[81920];
        var totalBytesRead = 0L;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancelToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancelToken);
            totalBytesRead += bytesRead;

            if (totalBytes > 0)
            {
                var percentage = (int)(totalBytesRead * 100 / totalBytes);
                progress(percentage);
            }
        }

        progress(100);
        logger.Info($"Download completed: {localFile}");
    }

    /// <summary>
    /// 构建文件名: {PackageId}-{Version}-{Os}-{Arch}[-full|-delta].nupkg
    /// </summary>
    private static string BuildFileName(string packageId, string version, string os, string arch, UpdateKind kind)
    {
        var suffix = kind == UpdateKind.Incremental ? "-delta" : "-full";
        return $"{packageId}-{version}-{os}-{arch}{suffix}.nupkg";
    }
}
