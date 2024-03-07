using Polymerium.Trident.Engines.Launching;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Trident.Abstractions;

namespace Polymerium.Trident.Engines
{
    public class LaunchEngine : IAsyncEngine<Scrap>
    {
        private const int TIME_DELAY = 500;

        private readonly Regex pattern =
            new(
                @"\[(.*)\] \[(?<thread>[a-zA-Z0-9\ \-#@]+)/(?<level>[a-zA-Z]+)\](\ \[(?<source>[a-zA-Z0-9\ \\./\-]+)\])?: (?<message>.*)");

        private Process? inner;

        public async IAsyncEnumerator<Scrap> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (inner == null)
            {
                yield break;
            }

            while (!inner.HasExited && !cancellationToken.IsCancellationRequested)
            {
                if (!inner.StandardOutput.EndOfStream)
                {
                    string? line = await inner.StandardOutput.ReadLineAsync();
                    if (line != null)
                    {
                        yield return TryConstruct(line);
                    }
                }
                else if (!inner.StandardError.EndOfStream)
                {
                    string? line = await inner.StandardError.ReadLineAsync();
                    if (line != null)
                    {
                        yield return TryConstruct(line);
                    }
                }
                else
                {
                    await Task.Delay(TIME_DELAY);
                }
            }
        }

        public void SetTarget(Process process)
        {
            inner = process;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
        }

        private Scrap TryConstruct(string data)
        {
            Match match = pattern.Match(data);
            if (match.Success && match.Groups.TryGetValue("level", out Group? level) &&
                match.Groups.TryGetValue("thread", out Group? thread) &&
                match.Groups.TryGetValue("message", out Group? message))
            {
                match.Groups.TryGetValue("source", out Group? sender);
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