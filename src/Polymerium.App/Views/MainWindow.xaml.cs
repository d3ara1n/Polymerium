// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace Polymerium.App.Views;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            SetTitleBar(Main.TitleBarDragArea);
            ExtendsContentIntoTitleBar = true;
        }
        else
        {
            (Main.ColumnRight.Width, Main.ColumnLeft.Width) = (
                Main.ColumnLeft.Width,
                Main.ColumnRight.Width
            );
        }

        if (Environment.OSVersion.Version.Major >= 10)
        {
            if (Environment.OSVersion.Version.Build >= 22000)
                Backdrop = new MicaSystemBackdrop();
            else
                Backdrop = new AcrylicSystemBackdrop();
        }
        else
        {
            Main.FakeBackground.Visibility = Visibility.Visible;
        }
    }
}