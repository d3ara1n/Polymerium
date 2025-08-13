using System.Threading.Tasks;
using Avalonia;
using Avalonia.Markup.Xaml;

namespace Polymerium.App.Widgets
{
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

        protected override Task OnDeinitializeAsync()
        {
            Context.SetLocalData(nameof(NoteText), NoteText);
            return Task.CompletedTask;
        }

        protected override Task OnInitializeAsync()
        {
            NoteText = Context.GetLocalData<string>(nameof(NoteText)) ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
