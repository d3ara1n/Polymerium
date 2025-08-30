using System;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;

namespace Polymerium.App.Models;

public static class InternalCommands
{
    public static ICommand OpenUriCommand { get; } = new RelayCommand<Uri>(uri =>
    {
        if (uri != null && uri.IsAbsoluteUri)
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchUriAsync(uri);
        }
    });

    public static ICommand OpenFolderCommand { get; } = new RelayCommand<string>(path =>
    {
        if (path != null && Directory.Exists(path))
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(path));
        }
    });
}
