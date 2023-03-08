using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Services;

namespace Polymerium.App.Controls;

public class Drawer : ContentControl
{
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(Drawer), new PropertyMetadata(string.Empty));

    public IOverlayService? OverlayService { get; set; }

    public Drawer()
    {
        DefaultStyleKey = typeof(Drawer);
    }

    protected override void OnApplyTemplate()
    {
        var button = (Button)GetTemplateChild("PART_CloseButton");
        button.Command = new RelayCommand(Close);
        var close = (Storyboard)GetTemplateChild("PART_CloseStoryBoard");
        close.Completed += (_, _) => OverlayService!.Dismiss();
        VisualStateManager.GoToState(this, "Opened", false);
    }

    protected virtual void OnClosing()
    {
    }

    public void Close()
    {
        OnClosing();
        VisualStateManager.GoToState(this, "Closed", false);
    }
}