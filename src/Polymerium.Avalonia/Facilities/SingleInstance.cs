using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.Avalonia.Facilities;

// Named-Mutex single-instance guard. The second instance pings the first through a
// one-way named pipe; messages are single-line JSON so future navigation forwarding
// (polymerium://, args) only adds fields without touching the transport.
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
                // Listener teardown raced with shutdown — nothing to act on.
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
            // Best effort: the second instance must never hang the user — if the first
            // instance's server isn't reachable, just exit silently.
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
