using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Metadata;
using Huskui.Avalonia.Models;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":information", ":success", ":warning", ":danger")]
public class NotificationItem : ContentControl
{
    public static readonly StyledProperty<NotificationLevel> LevelProperty =
        AvaloniaProperty.Register<NotificationItem, NotificationLevel>(nameof(Level));

    public NotificationLevel Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public static readonly DirectProperty<NotificationItem, AvaloniaList<NotificationAction>> ActionsProperty =
        AvaloniaProperty.RegisterDirect<NotificationItem, AvaloniaList<NotificationAction>>(nameof(Actions),
            o => o.Actions, (o, v) => o.Actions = v);

    private AvaloniaList<NotificationAction> _actions = [];

    [Content]
    public AvaloniaList<NotificationAction> Actions
    {
        get => _actions;
        set => SetAndRaise(ActionsProperty, ref _actions, value);
    }

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<NotificationItem, string>(nameof(Title), string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }


    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<NotificationItem, string>(nameof(Message), string.Empty);

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DirectProperty<NotificationItem, object?> ParameterProperty =
        AvaloniaProperty.RegisterDirect<NotificationItem, object?>(nameof(Parameter), o => o.Parameter,
            (o, v) => o.Parameter = v);

    private object? _parameter;

    public object? Parameter
    {
        get => _parameter;
        set => SetAndRaise(ParameterProperty, ref _parameter, value);
    }

    protected override Type StyleKeyOverride { get; } = typeof(NotificationItem);

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
    }

    private void SetPseudoClass(string name)
    {
        foreach (var i in (string[]) [":information", ":success", ":warning", ":danger"])
            PseudoClasses.Set(i, name == i);
    }
}