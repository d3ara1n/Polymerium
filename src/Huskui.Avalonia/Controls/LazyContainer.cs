using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Huskui.Avalonia.Models;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ContentPresenter, typeof(ContentPresenter))]
public class LazyContainer : TemplatedControl
{
    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);

    public static readonly StyledProperty<object?> BadContentProperty =
        AvaloniaProperty.Register<LazyContainer, object?>(nameof(BadContent));

    public static readonly StyledProperty<bool> IsBadProperty =
        AvaloniaProperty.Register<LazyContainer, bool>(nameof(IsBad));

    public static readonly StyledProperty<LazyObject?> SourceProperty =
        AvaloniaProperty.Register<LazyContainer, LazyObject?>(nameof(Source));

    public static readonly StyledProperty<IDataTemplate?> SourceTemplateProperty =
        AvaloniaProperty.Register<LazyContainer, IDataTemplate?>(nameof(SourceTemplate));

    private ContentPresenter? _contentPresenter;

    public LazyObject? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public IDataTemplate? SourceTemplate
    {
        get => GetValue(SourceTemplateProperty);
        set => SetValue(SourceTemplateProperty, value);
    }


    [Content]
    public object? BadContent
    {
        get => GetValue(BadContentProperty);
        set => SetValue(BadContentProperty, value);
    }

    public bool IsBad
    {
        get => GetValue(IsBadProperty);
        set => SetValue(IsBadProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _contentPresenter = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter);
    }


    protected override async void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceProperty)
        {
            if (change.OldValue is LazyObject { IsCancelled: false, IsInProgress: true } old)
                old.Cancel();
            if (change.NewValue is LazyObject lazy)
                await LoadAsync(lazy);
        }
    }

    private async Task LoadAsync(LazyObject lazy)
    {
        ArgumentNullException.ThrowIfNull(_contentPresenter);

        _contentPresenter.Content = null;
        _contentPresenter.ContentTemplate = null;
        IsBad = false;
        if (lazy.Value != null)
        {
            _contentPresenter.Content = lazy.Value;
            _contentPresenter.ContentTemplate = SourceTemplate;
        }
        else
            try
            {
                await lazy.FetchAsync();
                _contentPresenter.Content = lazy.Value;
                _contentPresenter.ContentTemplate = SourceTemplate;
            }
            catch
            {
                IsBad = true;
            }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (Source is { IsCancelled: false, IsInProgress: true })
            Source.Cancel();
    }
}