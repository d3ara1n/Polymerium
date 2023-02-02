// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Dialogs;

public sealed partial class TextInputDialog : ContentDialog
{
    // Using a DependencyProperty as the backing store for InputText.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty InputTextProperty =
        DependencyProperty.Register(nameof(InputText), typeof(string), typeof(TextInputDialog),
            new PropertyMetadata(string.Empty));

    // Using a DependencyProperty as the backing store for InputTextPlaceholder.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty InputTextPlaceholderProperty =
        DependencyProperty.Register(nameof(InputTextPlaceholder), typeof(string), typeof(TextInputDialog),
            new PropertyMetadata(string.Empty));

    // Using a DependencyProperty as the backing store for Description.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(TextInputDialog),
            new PropertyMetadata(string.Empty));


    public TextInputDialog()
    {
        InitializeComponent();
    }

    public string InputText
    {
        get => (string)GetValue(InputTextProperty);
        set => SetValue(InputTextProperty, value);
    }


    public string InputTextPlaceholder
    {
        get => (string)GetValue(InputTextPlaceholderProperty);
        set => SetValue(InputTextPlaceholderProperty, value);
    }


    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}