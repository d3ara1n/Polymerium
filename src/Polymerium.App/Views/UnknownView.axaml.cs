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
    public static readonly DirectProperty<UnknownView, AvaloniaList<NotificationAction>> ActionsProperty =
        AvaloniaProperty.RegisterDirect<UnknownView, AvaloniaList<NotificationAction>>(nameof(Actions),
            o => o.Actions,
            (o, v) => o.Actions = v);

    public UnknownView()
    {
        InitializeComponent();
        Actions =
        [
            new NotificationAction("Information",
                                   new RelayCommand<NotificationItem>(x =>
                                   {
                                       if (x is not null)
                                           x.Level = NotificationLevel.Information;
                                   }),
                                   Notification),
            new NotificationAction("Success",
                                   new RelayCommand<NotificationItem>(x =>
                                   {
                                       if (x is not null)
                                           x.Level = NotificationLevel.Success;
                                   }),
                                   Notification),
            new NotificationAction("Warning",
                                   new RelayCommand<NotificationItem>(x =>
                                   {
                                       if (x is not null)
                                           x.Level = NotificationLevel.Warning;
                                   }),
                                   Notification),
            new NotificationAction("Danger",
                                   new RelayCommand<NotificationItem>(x =>
                                   {
                                       if (x is not null)
                                           x.Level = NotificationLevel.Danger;
                                   }),
                                   Notification)
        ];
    }

    public AvaloniaList<NotificationAction> Actions
    {
        get;
        set => SetAndRaise(ActionsProperty, ref field, value);
    }


    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
        {
            if (Application.Current.ActualThemeVariant == ThemeVariant.Light)
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            else
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
        }
    }
}