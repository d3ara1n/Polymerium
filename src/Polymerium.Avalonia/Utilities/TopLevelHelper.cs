using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Huskui.Avalonia.Models;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.Utilities;

public static class TopLevelHelper
{
    /// <summary>
    ///     从当前应用生命周期中获取活跃主窗口的 TopLevel。
    ///     正常运行时主窗口一定存在；走到无窗口分支意味着在窗口关闭后仍有调用，属于不应发生的异常路径。
    /// </summary>
    public static TopLevel GetTopLevel()
    {
        if (
            Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return TopLevel.GetTopLevel(window)
                ?? throw new UnreachableException(
                    "TopLevel is null for the active main window."
                );
        }

        throw new UnreachableException("No active main window to resolve a TopLevel from.");
    }

    public static Task LaunchUriAsync(
        TopLevel? topLevel,
        Uri uri,
        string errorTitle,
        NotificationService? notificationService = null,
        GrowlLevel level = GrowlLevel.Warning,
        Uri? thumbnail = null
    ) =>
        LaunchAsync(
            topLevel,
            launcher => launcher.LaunchUriAsync(uri),
            errorTitle,
            notificationService,
            level,
            thumbnail
        );

    public static Task LaunchFileInfoAsync(
        TopLevel? topLevel,
        FileInfo file,
        string errorTitle,
        NotificationService? notificationService = null,
        GrowlLevel level = GrowlLevel.Warning,
        Uri? thumbnail = null
    ) =>
        LaunchAsync(
            topLevel,
            launcher => launcher.LaunchFileInfoAsync(file),
            errorTitle,
            notificationService,
            level,
            thumbnail
        );

    public static Task LaunchDirectoryInfoAsync(
        TopLevel? topLevel,
        DirectoryInfo directory,
        string errorTitle,
        NotificationService? notificationService = null,
        GrowlLevel level = GrowlLevel.Warning,
        Uri? thumbnail = null
    ) =>
        LaunchAsync(
            topLevel,
            launcher => launcher.LaunchDirectoryInfoAsync(directory),
            errorTitle,
            notificationService,
            level,
            thumbnail
        );

    private static async Task LaunchAsync(
        TopLevel? topLevel,
        Func<ILauncher, Task> operation,
        string errorTitle,
        NotificationService? notificationService,
        GrowlLevel level,
        Uri? thumbnail
    )
    {
        var service = notificationService ?? Program.Services?.GetService<NotificationService>();
        var launcher = topLevel?.Launcher;

        if (launcher is null)
        {
            service?.PopMessage(
                "Launcher is unavailable.",
                errorTitle,
                level,
                thumbnail: thumbnail
            );
            return;
        }

        try
        {
            await operation(launcher);
        }
        catch (Exception ex)
        {
            service?.PopMessage(ex, errorTitle, level, thumbnail: thumbnail);
        }
    }

    public static async Task CopyToClipboardAsync(
        TopLevel? topLevel,
        string text,
        string errorTitle,
        NotificationService? notificationService = null,
        GrowlLevel level = GrowlLevel.Warning,
        Uri? thumbnail = null
    )
    {
        var service = notificationService ?? Program.Services?.GetService<NotificationService>();
        var clipboard = topLevel?.Clipboard;

        if (clipboard is null)
        {
            service?.PopMessage(
                "Clipboard is unavailable.",
                errorTitle,
                level,
                thumbnail: thumbnail
            );
            return;
        }

        try
        {
            await clipboard.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            service?.PopMessage(ex, errorTitle, level, thumbnail: thumbnail);
        }
    }
}
