// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using Windows.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Polymerium.App.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Title = "Polymerium";

        AppWindow.Resize(new SizeInt32(780, 600));

        if (Environment.OSVersion.Version.Major >= 10)
        {
            if (Environment.OSVersion.Version.Build >= 22000)
            {
                SystemBackdrop = new MicaBackdrop();
                SetTitleBar(Main.TitleBarDragArea);
                ExtendsContentIntoTitleBar = true;
            }
            else
            {
                SystemBackdrop = new DesktopAcrylicBackdrop();
            }
        }
        else
        {
            // only windows 10+ supports msix so...
            (Main.ColumnRight.Width, Main.ColumnLeft.Width) = (
                Main.ColumnLeft.Width,
                Main.ColumnRight.Width
            );
        }
    }
}