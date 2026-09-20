using Avalonia;
using Avalonia.Controls;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Components;

public partial class ChangelogView : UserControl
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<ChangelogView, string?>(nameof(Markdown));

    public static readonly DirectProperty<ChangelogView, ChangelogDocumentModel?> DocumentProperty =
        AvaloniaProperty.RegisterDirect<ChangelogView, ChangelogDocumentModel?>(nameof(Document), o => o.Document);

    public ChangelogView() => InitializeComponent();

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public ChangelogDocumentModel? Document
    {
        get;
        private set => SetAndRaise(DocumentProperty, ref field, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MarkdownProperty)
        {
            Document = ChangelogParserHelper.Parse(change.GetNewValue<string?>());
        }
    }
}
