using Avalonia;
using Avalonia.Controls;
using FluentIcons.Common;

namespace Polymerium.App.Controls;

public class SettingsEntry : ItemsControl
{
    public static readonly StyledProperty<Symbol> IconProperty =
        AvaloniaProperty.Register<SettingsEntry, Symbol>(nameof(Icon));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<SettingsEntry, string>(nameof(Title));

    public static readonly StyledProperty<string> SummaryProperty =
        AvaloniaProperty.Register<SettingsEntry, string>(nameof(Summary));

    public Symbol Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Summary
    {
        get => GetValue(SummaryProperty);
        set => SetValue(SummaryProperty, value);
    }
}