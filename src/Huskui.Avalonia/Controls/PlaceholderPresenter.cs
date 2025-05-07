using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ContentPresenter, typeof(ContentPresenter))]
public class PlaceholderPresenter : TemplatedControl
{
    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);

    public static readonly StyledProperty<object?> PlaceholderProperty =
        AvaloniaProperty.Register<PlaceholderPresenter, object?>(nameof(Placeholder));

    public static readonly StyledProperty<object?> SourceProperty =
        AvaloniaProperty.Register<PlaceholderPresenter, object?>(nameof(Source));

    public static readonly StyledProperty<IDataTemplate?> SourceTemplateProperty =
        AvaloniaProperty.Register<PlaceholderPresenter, IDataTemplate?>(nameof(SourceTemplate));

    private ContentPresenter? _contentPresenter;

    [Content]
    public object? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public object? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public IDataTemplate? SourceTemplate
    {
        get => GetValue(SourceTemplateProperty);
        set => SetValue(SourceTemplateProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _contentPresenter = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter);

        UpdateContent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty || change.Property == SourceTemplateProperty)
            if (_contentPresenter != null)
                UpdateContent();
    }

    private void UpdateContent()
    {
        ArgumentNullException.ThrowIfNull(_contentPresenter);

        var source = Source;
        var template = SourceTemplate;


        if (_contentPresenter.Content is ILogical oldLogical)
            LogicalChildren.Remove(oldLogical);

        if (source != null)
        {
            _contentPresenter.ContentTemplate = template;
            _contentPresenter.Content = source;
        }
        else
        {
            _contentPresenter.ContentTemplate = null;
            _contentPresenter.Content = Placeholder;
        }

        if (_contentPresenter.Content is ILogical newLogical)
            LogicalChildren.Add(newLogical);
    }
}