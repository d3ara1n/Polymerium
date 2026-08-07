using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Models;

public static class InternalCommands
{
    public static ICommand OpenUriCommand { get; } = new AsyncRelayCommand<Uri>(uri =>
    {
        if (uri != null && uri.IsAbsoluteUri)
        {
            return TopLevelHelper.LaunchUriAsync(TopLevelHelper.GetTopLevel(),
                                                 uri,
                                                 LanguageManager.Instance.InternalCommands_OpenLinkDangerNotificationTitle.Current());
        }

        return Task.CompletedTask;
    });

    public static ICommand OpenStringUriCommand { get; } = new AsyncRelayCommand<string>(str =>
    {
        if (Uri.IsWellFormedUriString(str, UriKind.Absolute))
        {
            return TopLevelHelper.LaunchUriAsync(TopLevelHelper.GetTopLevel(),
                                                 new(str),
                                                 LanguageManager.Instance.InternalCommands_OpenLinkDangerNotificationTitle.Current());
        }

        return Task.CompletedTask;
    });

    public static ICommand OpenFolderCommand { get; } = new AsyncRelayCommand<string>(path =>
    {
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path);
        }

        if (path != null && Directory.Exists(path))
        {
            return TopLevelHelper.LaunchDirectoryInfoAsync(TopLevelHelper.GetTopLevel(),
                                                           new(path),
                                                           LanguageManager.Instance.Shared_FailedToOpenFolderDangerNotificationTitle.Current());
        }

        return Task.CompletedTask;
    });

    public static ICommand CopyToClipboardCommand { get; } = new AsyncRelayCommand<string>(async text =>
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        await TopLevelHelper.CopyToClipboardAsync(TopLevelHelper.GetTopLevel(),
                                                  text,
                                                  LanguageManager.Instance.InternalCommands_CopyTextDangerNotificationTitle.Current());
    });
}
