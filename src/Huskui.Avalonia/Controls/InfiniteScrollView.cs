using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Huskui.Avalonia.Models;

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
            _scrollViewer?.ScrollToHome();
            _ = UpdateAsync();
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
        if (_scrollViewer == null || _pendingPresenter == null)
            return;

        if (offset.Y > _scrollViewer.ScrollBarMaximum.Y - _pendingPresenter.Bounds.Height)
            _ = UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        do
        {
            if (_source == null)
            {
                if (_pendingPresenter != null)
                    _pendingPresenter.IsVisible = false;

                return;
            }

            if (_source.IsFetching)
                return;

            if (!_source.HasNext)
                return;

            if (_pendingPresenter != null)
                _pendingPresenter.IsVisible = true;

            PseudoClasses.Set(":loading", false);
            PseudoClasses.Set(":finished", false);
            PseudoClasses.Set(":idle", false);

            try
            {
                await _source.FetchAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            if (!_source.HasNext)
            {
                if (_pendingPresenter != null)
                    _pendingPresenter.IsVisible = false;
                return;
            }

            PseudoClasses.Set(":loading", false);
            PseudoClasses.Set(!_source.HasNext ? ":finished" : ":idle", true);

            // 直接判断是否循环会因为元素还没生成滚动条还在原位而被迫继续刷新
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        } while (_source.HasNext
              && _scrollViewer is not null
              && _pendingPresenter is not null
              && _scrollViewer.Offset.Y > _scrollViewer.ScrollBarMaximum.Y - _pendingPresenter.Bounds.Height);
    }
}