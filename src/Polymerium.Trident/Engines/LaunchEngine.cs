using Polymerium.Trident.Engines.Launching;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Trident.Abstractions;

namespace Polymerium.Trident.Engines
{
    public class LaunchEngine : IAsyncEngine<Scrap>
    {
        private Process? inner;

        public IAsyncEnumerator<Scrap> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inner);
            return new LaunchEngineEnumerator(inner, cancellationToken);
        }

        public void SetTarget(Process process)
        {
            inner = process;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
        }

        public class LaunchEngineEnumerator : IAsyncEnumerator<Scrap>
        {
            private const int TIME_DELAY = 500;
            private readonly CancellationToken cancellationToken;

            private readonly Process inner;

            private readonly Regex pattern =
                new(
                    @"\[(.*)\] \[(?<thread>[a-zA-Z0-9\ \-#@]+)/(?<level>[a-zA-Z]+)\](\ \[(?<source>[a-zA-Z0-9\ \\./\-]+)\])?: (?<message>.*)");

            // Send 但不 Sync
            private readonly Queue<string> queue = new();

            internal LaunchEngineEnumerator(Process process, CancellationToken token = default)
            {
                cancellationToken = token;
                inner = process;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            public Scrap Current { get; private set; } = null!;

            public ValueTask DisposeAsync()
            {
                inner.CancelErrorRead();
                inner.CancelOutputRead();

                // inner.Close()
                // it throws exception for some reason
                return ValueTask.CompletedTask;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                while (!inner.HasExited && !cancellationToken.IsCancellationRequested)
                {
                    if (queue.TryDequeue(out var line) && !string.IsNullOrEmpty(line))
                    {
                        Current = TryConstruct(line);
                        return true;
                    }

                    await Task.Delay(TIME_DELAY);
                }

                Current = null!;
                return false;
            }

            private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    queue.Enqueue(e.Data);
                }
            }

            private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    queue.Enqueue(e.Data);
                }
            }

            private Scrap TryConstruct(string data)
            {
                var match = pattern.Match(data);
                if (match.Success && match.Groups.TryGetValue("level", out var level) &&
                    match.Groups.TryGetValue("thread", out var thread) &&
                    match.Groups.TryGetValue("message", out var message))
                {
                    match.Groups.TryGetValue("source", out var sender);
                    return new Scrap(level.Value.ToUpper() switch
                    {
                        "INFO" => ScrapLevel.Information,
                        "WARN" => ScrapLevel.Warning,
                        "ERROR" => ScrapLevel.Error,
                        _ => ScrapLevel.Information
                    }, DateTimeOffset.Now, thread.Value, sender?.Value, message.Value);
                }

                return new Scrap(ScrapLevel.Information, DateTimeOffset.Now, "*", null, data);
            }
        }
    }
}