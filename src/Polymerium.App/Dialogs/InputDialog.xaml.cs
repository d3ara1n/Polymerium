// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;

namespace Polymerium.App.Dialogs;

public sealed partial class InputDialog
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(InputDialog), new PropertyMetadata(default(string)));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(InputDialog), new PropertyMetadata("Input something..."));

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(InputDialog), new PropertyMetadata(default(string)));

    public InputDialog(XamlRoot root)
    {
        XamlRoot = root;
        InitializeComponent();
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Result => Validate() ? Text : Placeholder;

    private bool Validate()
    {
        return !string.IsNullOrEmpty(Text);
    }
}