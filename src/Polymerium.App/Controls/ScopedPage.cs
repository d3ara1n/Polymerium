using System;
using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Controls;

public class ScopedPage : Page
{
    public static readonly DirectProperty<ScopedPage, bool> IsMaximizedProperty =
        AppWindow.IsMaximizedProperty.AddOwner<ScopedPage>(o => o.IsMaximized, (o, v) => o.IsMaximized = v);

    private bool isMaximized;

    public bool IsMaximized
    {
        get => isMaximized;
        set => SetAndRaise(IsMaximizedProperty, ref isMaximized, value);
    }

    protected override Type StyleKeyOverride => typeof(ScopedPage);
}