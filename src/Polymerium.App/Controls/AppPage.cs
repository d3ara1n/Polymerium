using System;
using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Controls;

public class AppPage : Page
{
    public static readonly DirectProperty<AppPage, bool> IsMaximizedProperty =
        AppWindow.IsMaximizedProperty.AddOwner<AppPage>(o => o.IsMaximized,
            (o, v) => o.IsMaximized = v);

    private bool isMaximized;

    public bool IsMaximized
    {
        get => isMaximized;
        set => SetAndRaise(IsMaximizedProperty, ref isMaximized, value);
    }

    protected override Type StyleKeyOverride => typeof(AppPage);
}