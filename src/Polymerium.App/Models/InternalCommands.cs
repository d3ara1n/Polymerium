using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Services;

namespace Polymerium.App.Models;

public static class InternalCommands
{
    public static ICommand OpenUriCommand { get; } =
        new RelayCommand<Uri>(uri =>
        {
            if (uri != null && uri.IsAbsoluteUri)
            {
                TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchUriAsync(uri);
            }
        });

    public static ICommand OpenStringUriCommand { get; } =
        new RelayCommand<string>(str =>
        {
            if (Uri.IsWellFormedUriString(str, UriKind.Absolute))
            {
                TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchUriAsync(new Uri(str));
            }
        });

    public static ICommand OpenFolderCommand { get; } =
        new RelayCommand<string>(path =>
        {
            if (path != null && Directory.Exists(path))
            {
                TopLevel
                    .GetTopLevel(MainWindow.Instance)
                    ?.Launcher.LaunchDirectoryInfoAsync(new(path));
            }
        });

    public static ICommand CopyToClipboardCommand { get; } =
        new AsyncRelayCommand<string>(async text =>
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                var task = TopLevel.GetTopLevel(MainWindow.Instance)?.Clipboard?.SetTextAsync(text);
                if (task != null)
                {
                    await task;
                }
            }
            catch (Exception) { }
        });
}
