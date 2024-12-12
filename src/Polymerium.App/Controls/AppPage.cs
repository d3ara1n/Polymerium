using System;
using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Controls;

public class AppPage : Page
{
    public static readonly DirectProperty<AppPage, bool> IsWindowMaximizedProperty =
        AppWindow.IsWindowMaximizedProperty.AddOwner<AppPage>(o => o.IsWindowMaximized,
            (o, v) => o.IsWindowMaximized = v);

    private bool isWindowMaximized;

    public bool IsWindowMaximized
    {
        get => isWindowMaximized;
        set => SetAndRaise(IsWindowMaximizedProperty, ref isWindowMaximized, value);
    }

    protected override Type StyleKeyOverride => typeof(AppPage);
}