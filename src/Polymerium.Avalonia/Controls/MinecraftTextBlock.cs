using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using TridentCore.Text;

namespace Polymerium.Avalonia.Controls;

[TemplatePart(PART_TextBlock, typeof(TextBlock))]
public class MinecraftTextBlock : TemplatedControl
{
    public const string PART_TextBlock = nameof(PART_TextBlock);

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MinecraftTextBlock, string?>(nameof(Text));

    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        AvaloniaProperty.Register<MinecraftTextBlock, TextWrapping>(nameof(TextWrapping));

    public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
        AvaloniaProperty.Register<MinecraftTextBlock, TextTrimming>(nameof(TextTrimming),
                                                                    TextTrimming.CharacterEllipsis);

    public static readonly StyledProperty<int?> MaxLinesProperty =
        AvaloniaProperty.Register<MinecraftTextBlock, int?>(nameof(MaxLines));

    private TextBlock? _textBlock;

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    public int? MaxLines
    {
        get => GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _textBlock = e.NameScope.Find<TextBlock>(PART_TextBlock);
        Render();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            Render();
        }
    }

    private void Render()
    {
        if (_textBlock is null)
        {
            return;
        }

        // NOTE: MinecraftTextReader.TryParse is strict only for JSON (syntax error -> false);
        // legacy § strings always parse. On JSON failure the raw text is shown verbatim,
        // matching MinecraftTextBlock's TryParse ? BuildInlines : BuildText contract.
        if (MinecraftTextReader.TryParse(Text, out var model))
        {
            BuildInlines(model);
        }
        else
        {
            _textBlock.Text = Text;
        }
    }

    private void BuildInlines(MinecraftText model)
    {
        if (_textBlock is not { } textBlock || textBlock.Inlines is not { } inlines)
        {
            return;
        }

        // NOTE: TextBlock renders Inlines when non-empty and setting Text clears Inlines,
        // so populate Inlines and drop Text to make Inlines the single source here.
        inlines.Clear();
        textBlock.Text = null;
        foreach (var run in model.Runs)
        {
            inlines.Add(ToInline(run));
        }
    }

    private static Run ToInline(MinecraftTextRun run)
    {
        var style = run.Style;
        var text = style.Obfuscated == true ? new('\u2588', run.Text.Length) : run.Text;
        var inline = new Run { Text = text };

        if (style.Color is { } color)
        {
            inline.Foreground = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
        }

        if (style.Bold == true)
        {
            inline.FontWeight = FontWeight.Bold;
        }

        if (style.Italic == true)
        {
            inline.FontStyle = FontStyle.Italic;
        }

        if (style.Underlined == true || style.Strikethrough == true)
        {
            inline.TextDecorations = DecorationsFor(style);
        }

        return inline;
    }

    private static TextDecorationCollection DecorationsFor(MinecraftTextStyle style)
    {
        var decos = new TextDecorationCollection();
        if (style.Underlined == true)
        {
            foreach (var d in TextDecorations.Underline)
            {
                decos.Add(d);
            }
        }

        if (style.Strikethrough == true)
        {
            foreach (var d in TextDecorations.Strikethrough)
            {
                decos.Add(d);
            }
        }

        return decos;
    }
}
