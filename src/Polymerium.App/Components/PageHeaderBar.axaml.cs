﻿using Avalonia;
using Avalonia.Controls;

namespace Polymerium.App.Components;

public partial class PageHeaderBar : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<PageHeaderBar, string>(nameof(Title), string.Empty);

    public PageHeaderBar() => InitializeComponent();

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}