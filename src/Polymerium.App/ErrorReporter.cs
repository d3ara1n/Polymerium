using System;
using System.IO;
using System.Reflection;
using System.Text;
using Sentry;

namespace Polymerium.App;

internal static class ErrorReporter
{
    internal enum ErrorReportSource
    {
        AppDomainUnhandled,
        DispatcherUnhandled,
        TaskUnobserved,
        LifetimeStartup,
        LifetimeShutdown,
    }

    internal readonly record struct ErrorReportMeta(
        ErrorReportSource Source,
        string Phase,
        bool Critical,
        bool Terminating,
        SentryLevel Level
    );

    public static void Report(object core, ErrorReportMeta meta)
    {
        if (!meta.Critical)
        {
            return;
        }

        if (core is Exception ex)
        {
            SentrySdk.CaptureException(
                ex,
                scope =>
                {
                    scope.Level = meta.Level;
                    // 可搜索、可筛选
                    scope.SetTag("polymerium.source", meta.Source.ToString());
                    scope.SetTag("polymerium.phase", meta.Phase);
                    scope.SetTag("polymerium.critical", meta.Critical ? "true" : "false");
                    scope.SetTag("polymerium.likely_crash", meta.Terminating ? "true" : "false");
                    // 辅助信息
                    scope.SetExtra(
                        "exception.type.full",
                        ex.GetType().FullName ?? ex.GetType().Name
                    );
                    scope.SetExtra("exception.message", ex.Message);
                    scope.SetExtra("exception.is_aggregate", ex is AggregateException);
                }
            );
        }

        Dump(core);
    }

    private static void Dump(object core)
    {
        // 只有调试模式才转储错误报告，而 Prod 模式有大概率文件目录是只读的
        if (!Program.IsDebug)
            return;

        var path = Path.Combine(
            AppContext.BaseDirectory,
            "dumps",
            $"Exception-{DateTimeOffset.Now.ToFileTime()}.log"
        );
        var sb = new StringBuilder(
            $"""
                                    // {DateTimeOffset.Now.ToString()}
                                    // Polymerium: {typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                                                  ?.InformationalVersion.Split('+')[0] ?? Program.Version}
                                    // Avalonia: {Assembly.GetEntryAssembly()?.GetName().Version}

                                    """
        );
        sb.AppendLine();
        DumpInternal(sb, core, 0);
        var dir = Path.GetDirectoryName(path);
        if (dir is not null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void DumpInternal(StringBuilder builder, object core, int level)
    {
        switch (core)
        {
            case AggregateException ae:
                builder.AppendLine(
                    $"""
                    --- LEVEL: {level} ---
                    Exception: {ae.GetType().Name}
                    Message: {ae.Message}
                    StackTrace: {ae.StackTrace}

                    """
                );
                foreach (var inner in ae.InnerExceptions)
                {
                    DumpInternal(builder, inner, level + 1);
                }

                if (ae.InnerException is not null)
                {
                    DumpInternal(builder, ae.InnerException, level + 1);
                }

                break;
            case Exception e:
                builder.AppendLine(
                    $"""
                    --- LEVEL: {level} ---
                    Exception: {e.GetType().Name}
                    Message: {e.Message}
                    StackTrace: {e.StackTrace}

                    """
                );
                if (e.InnerException is not null)
                {
                    DumpInternal(builder, e.InnerException, level + 1);
                }

                break;
            default:
                builder.AppendLine(
                    $"""
                    --- LEVEL: {level} ---
                    Content: {core.ToString()}

                    """
                );
                break;
        }
    }
}
