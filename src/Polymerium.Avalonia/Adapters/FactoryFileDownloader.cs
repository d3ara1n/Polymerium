using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Velopack.Sources;

namespace Polymerium.Avalonia.Adapters;

// NOTE: Velopack 自带的 HttpClientFileDownloader 会自建 handler，绕过 IHttpClientFactory，
//  使更新流量无视应用的代理设置。此适配器把下载路由回工厂管道（代理、UA、重试策略）。
public class FactoryFileDownloader(IHttpClientFactory factory) : IFileDownloader
{
    private const int BUFFER_SIZE = 81920;

    public async Task DownloadFile(string url,
                                   string targetFile,
                                   Action<int>? progress,
                                   IDictionary<string, string>? headers = null,
                                   double timeout = 30,
                                   CancellationToken cancelToken = default)
    {
        using var client = CreateClient(headers, timeout);
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancelToken);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;
        await using var download = await response.Content.ReadAsStreamAsync(cancelToken);
        await using var fs = File.Open(targetFile, FileMode.Create);

        if (progress == null || !contentLength.HasValue)
        {
            await download.CopyToAsync(fs, BUFFER_SIZE, cancelToken);
            return;
        }

        var buffer = new byte[BUFFER_SIZE];
        long totalBytesRead = 0;
        var lastProgress = 0;
        int bytesRead;
        while ((bytesRead = await download.ReadAsync(buffer, 0, buffer.Length, cancelToken)) > 0)
        {
            await fs.WriteAsync(buffer, 0, bytesRead, cancelToken);
            totalBytesRead += bytesRead;
            cancelToken.ThrowIfCancellationRequested();

            var curProgress = (int)((double)totalBytesRead / contentLength.Value * 100);
            if (curProgress - lastProgress >= 3)
            {
                lastProgress = curProgress;
                progress(curProgress);
            }
        }

        if (lastProgress < 100)
        {
            progress(100);
        }
    }

    public async Task<byte[]> DownloadBytes(string url,
                                            IDictionary<string, string>? headers = null,
                                            double timeout = 30)
    {
        using var client = CreateClient(headers, timeout);
        return await client.GetByteArrayAsync(url);
    }

    public async Task<string> DownloadString(string url,
                                             IDictionary<string, string>? headers = null,
                                             double timeout = 30)
    {
        using var client = CreateClient(headers, timeout);
        return await client.GetStringAsync(url);
    }

    private HttpClient CreateClient(IDictionary<string, string>? headers, double timeout)
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(timeout);
        foreach (var (key, value) in headers ?? new Dictionary<string, string>())
        {
            client.DefaultRequestHeaders.Add(key, value);
        }

        return client;
    }
}
