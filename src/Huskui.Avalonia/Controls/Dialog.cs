using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

public class Dialog : HeaderedContentControl
{
    public static readonly DirectProperty<Dialog, ICommand?> PrimaryCommandProperty =
        AvaloniaProperty.RegisterDirect<Dialog, ICommand?>(nameof(PrimaryCommand), o => o.PrimaryCommand,
            (o, v) => o.PrimaryCommand = v);

    public static readonly DirectProperty<Dialog, ICommand?> SecondaryCommandProperty =
        AvaloniaProperty.RegisterDirect<Dialog, ICommand?>(nameof(SecondaryCommand), o => o.SecondaryCommand,
            (o, v) => o.SecondaryCommand = v);

    public static readonly DirectProperty<Dialog, string> PrimaryTextProperty =
        AvaloniaProperty.RegisterDirect<Dialog, string>(nameof(PrimaryText), o => o.PrimaryText,
            (o, v) => o.PrimaryText = v);

    public static readonly DirectProperty<Dialog, string> SecondaryTextProperty =
        AvaloniaProperty.RegisterDirect<Dialog, string>(nameof(SecondaryText), o => o.SecondaryText,
            (o, v) => o.SecondaryText = v);

    public static readonly DirectProperty<Dialog, string> TitleProperty =
        AvaloniaProperty.RegisterDirect<Dialog, string>(nameof(Title), o => o.Title, (o, v) => o.Title = v);

    public static readonly DirectProperty<Dialog, string> MessageProperty =
        AvaloniaProperty.RegisterDirect<Dialog, string>(nameof(Message), o => o.Message, (o, v) => o.Message = v);

    private string _message = string.Empty;

    private ICommand? _primaryCommand;

    private string _primaryText = string.Empty;

    private ICommand? _secondaryCommand;

    private string _secondaryText = string.Empty;

    private string _title = string.Empty;
    protected override Type StyleKeyOverride { get; } = typeof(Dialog);

    public ICommand? PrimaryCommand
    {
        get => _primaryCommand;
        set => SetAndRaise(PrimaryCommandProperty, ref _primaryCommand, value);
    }

    public ICommand? SecondaryCommand
    {
        get => _secondaryCommand;
        set => SetAndRaise(SecondaryCommandProperty, ref _secondaryCommand, value);
    }

    public string PrimaryText
    {
        get => _primaryText;
        set => SetAndRaise(PrimaryTextProperty, ref _primaryText, value);
    }

    public string SecondaryText
    {
        get => _secondaryText;
        set => SetAndRaise(SecondaryTextProperty, ref _secondaryText, value);
    }

    public string Title
    {
        get => _title;
        set => SetAndRaise(TitleProperty, ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => SetAndRaise(MessageProperty, ref _message, value);
    }
}