using Avalonia;
using Avalonia.Markup.Xaml;

namespace Polymerium.App.Widgets;

public class NoteWidget : WidgetBase
{
    public static readonly DirectProperty<NoteWidget, string> NoteTextProperty =
        AvaloniaProperty.RegisterDirect<NoteWidget, string>(nameof(NoteText),
                                                            o => o.NoteText,
                                                            (o, v) => o.NoteText = v);


    public NoteWidget() => AvaloniaXamlLoader.Load(this);

    public string NoteText
    {
        get;
        set => SetAndRaise(NoteTextProperty, ref field, value);
    } = string.Empty;

    protected override void OnDeinitialize()
    {
        Context.SetLocalData(nameof(NoteText), NoteText);
    }

    protected override void OnInitialize()
    {
        NoteText = Context.GetLocalData<string>(nameof(NoteText)) ?? string.Empty;
    }
}