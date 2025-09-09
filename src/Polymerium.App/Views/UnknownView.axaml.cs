using Avalonia;
using Avalonia.Collections;
using Avalonia.Interactivity;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;

namespace Polymerium.App.Views;

public partial class UnknownView : Page
{
    public static readonly DirectProperty<UnknownView, AvaloniaList<GrowlAction>> ActionsProperty =
        AvaloniaProperty.RegisterDirect<UnknownView, AvaloniaList<GrowlAction>>(nameof(Actions),
                                                                                    o => o.Actions,
                                                                                    (o, v) => o.Actions = v);

    public UnknownView()
    {
        InitializeComponent();
        Actions =
        [
            new("Information",
                new RelayCommand<GrowlItem>(x =>
                {
                    x?.Level = GrowlLevel.Information;
                }),
                Notification),
            new("Success",
                new RelayCommand<GrowlItem>(x =>
                {
                    x?.Level = GrowlLevel.Success;
                }),
                Notification),
            new("Warning",
                new RelayCommand<GrowlItem>(x =>
                {
                    x?.Level = GrowlLevel.Warning;
                }),
                Notification),
            new("Danger",
                new RelayCommand<GrowlItem>(x =>
                {
                    x?.Level = GrowlLevel.Danger;
                }),
                Notification)
        ];
    }

    public AvaloniaList<GrowlAction> Actions
    {
        get;
        set => SetAndRaise(ActionsProperty, ref field, value);
    }


    private void ThemeSwitchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
        {
            if (Application.Current.ActualThemeVariant == ThemeVariant.Light)
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            }
            else
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            }
        }
    }
}
