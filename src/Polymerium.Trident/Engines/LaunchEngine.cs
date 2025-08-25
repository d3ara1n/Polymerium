using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Polymerium.Trident.Engines.Launching;

namespace Polymerium.Trident.Engines
{
    public partial class LaunchEngine : IAsyncEnumerable<Scrap>
    {
        private readonly Process _inner;

        public LaunchEngine(Process inner)
        {
            _inner = inner;
            _inner.StartInfo.RedirectStandardError = true;
            _inner.StartInfo.RedirectStandardOutput = true;
            _inner.EnableRaisingEvents = true;
        }

        #region IAsyncEnumerable<Scrap> Members

        public IAsyncEnumerator<Scrap> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(_inner);
            return new LaunchEngineEnumerator(_inner, cancellationToken);
        }

        #endregion

        #region Nested type: LaunchEngineEnumerator

        public partial class LaunchEngineEnumerator : IAsyncEnumerator<Scrap>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Channel<Scrap> _channel = Channel.CreateUnbounded<Scrap>();
            private readonly Process _inner;
            private readonly Regex _pattern = GenerateRegex();

            internal LaunchEngineEnumerator(Process process, CancellationToken token = default)
            {
                _cancellationToken = token;
                _inner = process;
                process.OutputDataReceived += ProcessOnOutputDataReceived;
                process.ErrorDataReceived += ProcessOnErrorDataReceived;
                process.Exited += ProcessOnExited;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            #region IAsyncEnumerator<Scrap> Members

            public Scrap Current { get; private set; } = null!;

            public ValueTask DisposeAsync()
            {
                _channel.Writer.TryComplete();
                _inner.CancelErrorRead();
                _inner.CancelOutputRead();

                _inner.EnableRaisingEvents = false;
                _inner.OutputDataReceived -= ProcessOnOutputDataReceived;
                _inner.ErrorDataReceived -= ProcessOnErrorDataReceived;
                _inner.Exited -= ProcessOnExited;

                // inner.Close()
                // it throws exception for some reason
                return ValueTask.CompletedTask;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                try
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    if (await _channel.Reader.WaitToReadAsync(_cancellationToken).ConfigureAwait(false))
                    {
                        if (_channel.Reader.TryRead(out var piece))
                        {
                            Current = piece;
                            return true;
                        }
                    }

                    return false;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }

            #endregion

            private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _channel.Writer.TryWrite(TryConstruct(e.Data));
                }
            }

            private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _channel.Writer.TryWrite(TryConstruct(e.Data));
                }
            }

            private void ProcessOnExited(object? sender, EventArgs e) => _channel.Writer.TryComplete();

            private Scrap TryConstruct(string data)
            {
                var match = _pattern.Match(data);
                if (match.Success
                 && match.Groups.TryGetValue("level", out var level)
                 && match.Groups.TryGetValue("thread", out var thread)
                 && match.Groups.TryGetValue("message", out var message))
                {
                    match.Groups.TryGetValue("source", out var sender);
                    return new(message.Value,
                               level.Value.ToUpper() switch
                               {
                                   "INFO" => ScrapLevel.Information,
                                   "WARN" => ScrapLevel.Warning,
                                   "ERROR" => ScrapLevel.Error,
                                   _ => ScrapLevel.Information
                               },
                               DateTimeOffset.Now,
                               thread.Value,
                               sender?.Value);
                }

                return new(data);
            }

            [GeneratedRegex(@"\[(.*)\] \[(?<thread>[a-zA-Z0-9\ \-#@]+)/(?<level>[a-zA-Z]+)\](\ \[(?<source>[a-zA-Z0-9\ \\./\-]+)\])?: (?<message>.*)")]
            private static partial Regex GenerateRegex();
        }

        #endregion
    }
}
