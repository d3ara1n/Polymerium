using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Services;
using System;

namespace Polymerium.App.Controls;

public class Drawer : HeaderedContentControl
{
    public Drawer()
    {
        DefaultStyleKey = typeof(Drawer);
    }

    public IOverlayService? OverlayService { get; set; }

    public event EventHandler<EventArgs>? Closed;

    protected override void OnApplyTemplate()
    {
        var button = (Button)GetTemplateChild("PART_CloseButton");
        button.Command = new RelayCommand(Close);
        var close = (Storyboard)GetTemplateChild("PART_CloseStoryBoard");
        close.Completed += (_, _) =>
        {
            OverlayService!.Dismiss();
            Closed?.Invoke(this, new EventArgs());
        };
        VisualStateManager.GoToState(this, "Opened", false);
    }

    protected virtual void OnClosing() { }

    public void Close()
    {
        OnClosing();
        VisualStateManager.GoToState(this, "Closed", false);
    }
}
