using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.Avalonia.Facilities;

// NOTE: Named-Mutex 单实例守卫——第二实例经单向命名管道 ping 第一实例；消息为单行 JSON，
//  未来导航转发（polymerium://、args）只加字段、不动传输层。
internal sealed class SingleInstance : IDisposable
{
    private const string MUTEX_NAME = "dev.dearain.Polymerium.single-instance";
    private const string PIPE_NAME = "dev.dearain.Polymerium.ipc";

    private readonly Mutex _mutex;
    private readonly bool _ownsMutex;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public SingleInstance()
    {
        _mutex = new(true, MUTEX_NAME, out var createdNew);
        _ownsMutex = createdNew;
        IsFirstInstance = createdNew;
    }

    public bool IsFirstInstance { get; }

    public void Dispose()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            try
            {
                _serverTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // NOTE: 监听器拆除与关闭竞态，无事可做。
            }

            _cts.Dispose();
        }

        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }

    public event Action<Message>? Received;

    public void StartServer()
    {
        _cts = new();
        _serverTask = Task.Run(() => RunServerAsync(_cts.Token));
    }

    private async Task RunServerAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await using var server = new NamedPipeServerStream(PIPE_NAME,
                                                               PipeDirection.In,
                                                               1,
                                                               PipeTransmissionMode.Byte,
                                                               PipeOptions.Asynchronous);

            try
            {
                await server.WaitForConnectionAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!server.IsConnected)
            {
                continue;
            }

            string? line;
            try
            {
                using var reader = new StreamReader(server, leaveOpen: true);
                line = await reader.ReadLineAsync(token);
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            Message? message;
            try
            {
                message = JsonSerializer.Deserialize<Message>(line);
            }
            catch
            {
                continue;
            }

            if (message is not null)
            {
                Received?.Invoke(message);
            }
        }
    }

    public static void Send(Message message)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.Out, PipeOptions.Asynchronous);
            client.Connect(3000);
            using var writer = new StreamWriter(client);
            writer.WriteLine(JsonSerializer.Serialize(message));
            writer.Flush();
        }
        catch
        {
            // NOTE: 尽力而为——第二实例绝不卡住用户，第一实例不可达时静默退出。
        }
    }

    public sealed class Message
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "activate";

        [JsonPropertyName("target")]
        public string? Target { get; set; }

        [JsonPropertyName("args")]
        public string[]? Args { get; set; }
    }
}
