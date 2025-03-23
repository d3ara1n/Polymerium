using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Interactivity;
using Huskui.Avalonia.Models;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":open", ":information", ":success", ":warning", ":danger")]
public class NotificationItem : ContentControl
{

    public static readonly StyledProperty<NotificationLevel> LevelProperty =
        AvaloniaProperty.Register<NotificationItem, NotificationLevel>(nameof(Level));

    public static readonly DirectProperty<NotificationItem, AvaloniaList<NotificationAction>> ActionsProperty =
        AvaloniaProperty.RegisterDirect<NotificationItem, AvaloniaList<NotificationAction>>(nameof(Actions),
            o => o.Actions,
            (o, v) => o.Actions = v);

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<NotificationItem, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<NotificationItem, bool>(nameof(IsOpen), true);

    public static readonly StyledProperty<bool> IsCloseButtonVisibleProperty =
        AvaloniaProperty.Register<NotificationItem, bool>(nameof(IsCloseButtonVisible));

    public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent =
        RoutedEvent.Register<NotificationItem, RoutedEventArgs>(nameof(Opened), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent =
        RoutedEvent.Register<NotificationItem, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<NotificationItem, double>(nameof(Progress));

    public static readonly StyledProperty<bool> IsProgressIndeterminateProperty =
        AvaloniaProperty.Register<NotificationItem, bool>(nameof(IsProgressIndeterminate));


    public static readonly StyledProperty<double> ProgressMaximumProperty =
        AvaloniaProperty.Register<NotificationItem, double>(nameof(ProgressMaximum), 100d);

    public static readonly StyledProperty<bool> IsProgressBarVisibleProperty =
        AvaloniaProperty.Register<NotificationItem, bool>(nameof(IsProgressBarVisible));


    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool IsCloseButtonVisible
    {
        get => GetValue(IsCloseButtonVisibleProperty);
        set => SetValue(IsCloseButtonVisibleProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool IsProgressIndeterminate
    {
        get => GetValue(IsProgressIndeterminateProperty);
        set => SetValue(IsProgressIndeterminateProperty, value);
    }

    public double ProgressMaximum
    {
        get => GetValue(ProgressMaximumProperty);
        set => SetValue(ProgressMaximumProperty, value);
    }

    public bool IsProgressBarVisible
    {
        get => GetValue(IsProgressBarVisibleProperty);
        set => SetValue(IsProgressBarVisibleProperty, value);
    }

    public NotificationLevel Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public AvaloniaList<NotificationAction> Actions
    {
        get;
        set => SetAndRaise(ActionsProperty, ref field, value);
    } = [];

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public event EventHandler<RoutedEventArgs>? Opened
    {
        add => AddHandler(OpenedEvent, value);
        remove => RemoveHandler(OpenedEvent, value);
    }

    public event EventHandler<RoutedEventArgs>? Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LevelProperty)
            SetPseudoClass(change.NewValue switch
            {
                NotificationLevel.Information => ":information",
                NotificationLevel.Success => ":success",
                NotificationLevel.Warning => ":warning",
                NotificationLevel.Danger => ":danger",
                _ => ":information"
            });

        if (change.Property == IsOpenProperty)
        {
            var opened = change.GetNewValue<bool>();
            if (opened)
                RaiseEvent(new RoutedEventArgs(OpenedEvent, this));
            PseudoClasses.Set(":open", opened);
        }

        if (change.Property == OpacityProperty && change.NewValue is < double.Epsilon && IsOpen == false)
            RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
    }

    public void Close() => IsOpen = false;

    private void SetPseudoClass(string name)
    {
        foreach (var i in (string[]) [":information", ":success", ":warning", ":danger"])
            PseudoClasses.Set(i, name == i);
    }
}