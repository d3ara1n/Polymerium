using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Huskui.Avalonia.Models;
using System.Diagnostics;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ScrollViewer, typeof(ScrollViewer))]
[TemplatePart(PART_PendingPresenter, typeof(ContentPresenter))]
[PseudoClasses(":idle", ":loading", ":finished")]
public class InfiniteScrollView : ItemsControl
{
    public const string PART_ScrollViewer = nameof(PART_ScrollViewer);
    public const string PART_PendingPresenter = nameof(PART_PendingPresenter);

    public static readonly StyledProperty<object?> PendingContentProperty =
        AvaloniaProperty.Register<InfiniteScrollView, object?>(nameof(PendingContent));

    public static readonly StyledProperty<IDataTemplate?> PendingContentTemplateProperty =
        AvaloniaProperty.Register<InfiniteScrollView, IDataTemplate?>(nameof(PendingContentTemplate));

    private ContentPresenter? _pendingPresenter;

    private ScrollViewer? _scrollViewer;
    private IInfiniteCollection? _source;

    public InfiniteScrollView() => PseudoClasses.Set(":idle", true);

    public object? PendingContent
    {
        get => GetValue(PendingContentProperty);
        set => SetValue(PendingContentProperty, value);
    }

    public IDataTemplate? PendingContentTemplate
    {
        get => GetValue(PendingContentTemplateProperty);
        set => SetValue(PendingContentTemplateProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            _source = change.NewValue as IInfiniteCollection;

            Update();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _scrollViewer = e.NameScope.Find<ScrollViewer>(PART_ScrollViewer);
        _pendingPresenter = e.NameScope.Find<ContentPresenter>(PART_PendingPresenter);
        _scrollViewer?.GetObservable(ScrollViewer.OffsetProperty).Subscribe(OnScroll);
    }

    private void OnScroll(Vector offset)
    {
        if (_scrollViewer == null || _pendingPresenter == null) return;

        if (offset.Y > _scrollViewer.ScrollBarMaximum.Y - _pendingPresenter.Bounds.Height) Update();
    }

    private void Update()
    {
        if (_source == null)
        {
            if (_pendingPresenter != null) _pendingPresenter.IsVisible = false;
            return;
        }

        if (_source.IsFetching) return;

        if (_pendingPresenter != null) _pendingPresenter.IsVisible = true;
        PseudoClasses.Set(":loading", false);

        Task.Run(_source.FetchAsync).ContinueWith(t =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                PseudoClasses.Set(":loading", false);

                if (!_source.HasNext)
                    PseudoClasses.Set(":finished", true);
                else
                    PseudoClasses.Set(":idle", true);
            });
            if (t.Exception != null) Debug.WriteLine(t.Exception);
        });
    }
}