﻿using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Dialogs;

namespace Polymerium.App.Services;

public class DialogService
{
    private XamlRoot XamlRoot
    {
        get
        {
            var xamlRoot = App.Current.Window.Content.XamlRoot;
            ArgumentNullException.ThrowIfNull(xamlRoot);
            return xamlRoot;
        }
    }

    public async Task<string?> RequestTextAsync(string message, string defaultValue)
    {
        var dialog = new InputDialog(XamlRoot)
        {
            Message = message,
            Placeholder = defaultValue
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) return dialog.Result;

        return null;
    }

    public async Task<bool> RequestConfirmationAsync(string message)
    {
        var dialog = new ConfirmDialog(XamlRoot)
        {
            Message = message
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}