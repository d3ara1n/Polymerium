using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Polymerium.App.Dialogs;

public sealed partial class ConfirmationDialog : ContentDialog
{
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(ConfirmationDialog),
            new PropertyMetadata(string.Empty));


    public ConfirmationDialog()
    {
        InitializeComponent();
    }
}