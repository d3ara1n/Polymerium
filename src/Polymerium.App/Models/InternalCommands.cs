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
using Polymerium.App.Utilities;

namespace Polymerium.App.Models;

public static class InternalCommands
{
    public static ICommand OpenUriCommand { get; } =
        new AsyncRelayCommand<Uri>(uri =>
        {
            if (uri != null && uri.IsAbsoluteUri)
            {
                return TopLevelHelper.LaunchUriAsync(
                    TopLevel.GetTopLevel(MainWindow.Instance),
                    uri,
                    "Failed to open link"
                );
            }

            return Task.CompletedTask;
        });

    public static ICommand OpenStringUriCommand { get; } =
        new AsyncRelayCommand<string>(str =>
        {
            if (Uri.IsWellFormedUriString(str, UriKind.Absolute))
            {
                return TopLevelHelper.LaunchUriAsync(
                    TopLevel.GetTopLevel(MainWindow.Instance),
                    new Uri(str),
                    "Failed to open link"
                );
            }

            return Task.CompletedTask;
        });

    public static ICommand OpenFolderCommand { get; } =
        new AsyncRelayCommand<string>(path =>
        {
            if (File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }

            if (path != null && Directory.Exists(path))
            {
                return TopLevelHelper.LaunchDirectoryInfoAsync(
                    TopLevel.GetTopLevel(MainWindow.Instance),
                    new(path),
                    "Failed to open folder"
                );
            }

            return Task.CompletedTask;
        });

    public static ICommand CopyToClipboardCommand { get; } =
        new AsyncRelayCommand<string>(async text =>
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            await TopLevelHelper.CopyToClipboardAsync(
                TopLevel.GetTopLevel(MainWindow.Instance),
                text,
                "Failed to copy text"
            );
        });
}
