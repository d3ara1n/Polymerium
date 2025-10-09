using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.App.Models;

namespace Polymerium.App.Utilities;

public static class NetworkCheckHelper
{
    private const int TIMEOUT_MILLISECONDS = 5000;
    private const int MAX_RETRIES = 3;

    /// <summary>
    ///     测试单个网站的连接性和延迟
    /// </summary>
    /// <param name="model">要测试的网站模型</param>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>测试是否成功</returns>
    public static async Task<bool> TestConnectionAsync(
        ConnectionTestSiteModel model,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        model.Status = ConnectionTestStatus.Testing;
        model.IsTesting = true;
        model.Latency = 0;
        model.ErrorMessage = null;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TIMEOUT_MILLISECONDS);

            // 使用 HEAD 请求来减少数据传输
            using var request = new HttpRequestMessage(HttpMethod.Head, model.Endpoint);
            using var response =
                await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            stopwatch.Stop();

            // 检查响应状态
            if (response.IsSuccessStatusCode
             || response.StatusCode == HttpStatusCode.MethodNotAllowed
             || response.StatusCode == HttpStatusCode.NotFound)
            {
                // 某些服务器可能不支持 HEAD 请求，返回 405，但这仍然表示服务器可达
                // 如果返回 404，对于 API 测试来说也行得通
                model.Latency = stopwatch.Elapsed.TotalMilliseconds;
                model.Status = ConnectionTestStatus.Success;
                model.IsTesting = false;
                return true;
            }
            else
            {
                model.Status = ConnectionTestStatus.Failed;
                model.ErrorMessage = $"HTTP {(int)response.StatusCode}";
                model.IsTesting = false;
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            model.Status = ConnectionTestStatus.Failed;
            model.ErrorMessage = "Timeout";
            model.IsTesting = false;
            return false;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            model.Status = ConnectionTestStatus.Failed;
            model.ErrorMessage = GetSimplifiedErrorMessage(ex);
            model.IsTesting = false;
            return false;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            model.Status = ConnectionTestStatus.Failed;
            model.ErrorMessage = ex.Message.Length > 50 ? ex.Message[..50] + "..." : ex.Message;
            model.IsTesting = false;
            return false;
        }
    }

    /// <summary>
    ///     测试多个网站的连接性
    /// </summary>
    /// <param name="models">要测试的网站模型列表</param>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功测试的数量</returns>
    public static async Task<int> TestConnectionsAsync(
        IEnumerable<ConnectionTestSiteModel> models,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        var successCount = 0;

        foreach (var model in models)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var success = await TestConnectionAsync(model, httpClient, cancellationToken);
            if (success)
            {
                successCount++;
            }

            // 在测试之间添加小延迟，避免过于频繁的请求
            await Task.Delay(100, cancellationToken);
        }

        return successCount;
    }

    /// <summary>
    ///     获取简化的错误消息
    /// </summary>
    private static string GetSimplifiedErrorMessage(HttpRequestException ex)
    {
        if (ex.InnerException != null)
        {
            var innerMessage = ex.InnerException.Message;
            if (innerMessage.Contains("No such host is known"))
            {
                return "DNS Failed";
            }

            if (innerMessage.Contains("Connection refused"))
            {
                return "Connection Refused";
            }

            if (innerMessage.Contains("Connection timed out"))
            {
                return "Timeout";
            }

            if (innerMessage.Contains("No connection could be made"))
            {
                return "No Connection";
            }
        }

        var message = ex.Message;
        if (message.Length > 50)
        {
            return message[..50] + "...";
        }

        return message;
    }

    /// <summary>
    ///     重置所有测试结果
    /// </summary>
    public static void ResetAll(IEnumerable<ConnectionTestSiteModel> models)
    {
        foreach (var model in models)
        {
            model.Status = ConnectionTestStatus.Pending;
            model.IsTesting = false;
            model.Latency = 0;
            model.ErrorMessage = null;
        }
    }
}
