using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using MirrorChyan.Net.Clients;
using MirrorChyan.Net.Models;
using Velopack;
using Velopack.Sources;

namespace VelopackExtension.MirrorChyan;

/// <summary>
/// MirrorChyan 更新源，实现 Velopack 的 IUpdateSource 接口
/// </summary>
public class MirrorChyanSource : IUpdateSource
{
    private readonly IMirrorChyanClient _client;
    private readonly string _productName;
    private readonly string _currentVersion;
    private readonly string? _cdk;
    private readonly string? _userAgent;

    /// <summary>
    /// 创建 MirrorChyan 更新源实例
    /// </summary>
    /// <param name="client">MirrorChyan 客户端</param>
    /// <param name="productName">产品名称（资源 ID）</param>
    /// <param name="currentVersion">当前版本</param>
    /// <param name="cdk">CDK 密钥（可选）</param>
    /// <param name="userAgent">User-Agent（可选）</param>
    public MirrorChyanSource(
        IMirrorChyanClient client,
        string productName,
        string currentVersion,
        string? cdk = null,
        string? userAgent = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _productName = productName ?? throw new ArgumentNullException(nameof(productName));
        _currentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
        _cdk = cdk;
        _userAgent = userAgent;
    }

    /// <summary>
    /// 获取发布更新信息
    /// </summary>
    public async Task<VelopackAssetFeed?> GetReleaseFeed(
        ILogger logger,
        string channel,
        Guid? stagingId = null,
        VelopackAsset? latestLocalRelease = null)
    {
        try
        {
            logger.LogInformation(
                "Checking for updates from MirrorChyan: Product={ProductName}, Channel={Channel}",
                _productName,
                channel);

            // 获取当前运行时信息
            var os = VelopackOses.FromVelopack(VelopackRuntimeInfo.SystemOs);
            var arch = VelopackArchs.FromVelopack(VelopackRuntimeInfo.SystemArch);

            // 调用 MirrorChyan API
            var response = await _client.GetLatestVersionAsync(
                rid: _productName,
                currentVersion: _currentVersion,
                cdk: _cdk,
                userAgent: _userAgent,
                os: os,
                arch: arch,
                channel: channel);

            // 检查响应
            if (response?.Data == null)
            {
                logger.LogWarning("MirrorChyan API returned no data");
                return null;
            }

            if (response.Code != 200)
            {
                logger.LogWarning(
                    "MirrorChyan API returned non-success code: {Code}, Message: {Message}",
                    response.Code,
                    response.Msg);
                return null;
            }

            var latest = response.Data;

            logger.LogInformation(
                "Found update: Version={Version}, Size={Size} bytes",
                latest.VersionName,
                latest.FileSize);

            // 构建 VelopackAsset
            var asset = new VelopackAsset
            {
                PackageId = _productName,
                Version = SemanticVersion.Parse(latest.VersionName),
                Type = latest.UpdateType == UpdateKind.Full
                    ? VelopackAssetType.Full
                    : VelopackAssetType.Delta,
                FileName = Path.GetFileName(latest.Url.LocalPath),
                SHA256 = latest.Sha256,
                Size = latest.FileSize,
                NotesMarkdown = latest.ReleaseNote,
                NotesHTML = null
            };

            // 创建下载函数
            async Task DownloadAsset(string assetPath, Action<int>? progress)
            {
                await DownloadFileAsync(latest.Url, assetPath, latest.Sha256, progress, logger);
            }

            return new VelopackAssetFeed
            {
                Assets = [asset],
                DownloadFunc = DownloadAsset
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get release feed from MirrorChyan");
            return null;
        }
    }

    /// <summary>
    /// 下载文件并验证 SHA256
    /// </summary>
    private static async Task DownloadFileAsync(
        Uri url,
        string targetPath,
        string expectedSha256,
        Action<int>? progress,
        ILogger logger)
    {
        logger.LogInformation("Downloading file from {Url} to {Path}", url, targetPath);

        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var buffer = new byte[8192];
        var bytesRead = 0L;

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(
            targetPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 8192,
            useAsync: true);

        int read;
        while ((read = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            bytesRead += read;

            // 报告进度
            if (totalBytes > 0 && progress != null)
            {
                var percentage = (int)(bytesRead * 100 / totalBytes);
                progress(percentage);
            }
        }

        await fileStream.FlushAsync();
        fileStream.Close();

        logger.LogInformation("Download completed. Verifying SHA256...");

        // 验证 SHA256
        var actualSha256 = await ComputeSha256Async(targetPath);
        if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(targetPath);
            throw new InvalidOperationException(
                $"SHA256 mismatch. Expected: {expectedSha256}, Actual: {actualSha256}");
        }

        logger.LogInformation("SHA256 verification successful");
    }

    /// <summary>
    /// 计算文件的 SHA256 哈希值
    /// </summary>
    private static async Task<string> ComputeSha256Async(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var fileStream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(fileStream);
        return Convert.ToHexString(hashBytes);
    }
}
