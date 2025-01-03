using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ScrollViewer, typeof(ScrollViewer))]
public class InfiniteScrollView : ItemsControl
{
    public const string PART_ScrollViewer = nameof(PART_ScrollViewer);

    private ScrollViewer? _scrollViewer;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _scrollViewer = e.NameScope.Find<ScrollViewer>(PART_ScrollViewer);

        _scrollViewer?.GetObservable(ScrollViewer.OffsetProperty).Subscribe(OnScroll);
    }

    private void OnScroll(Vector offset)
    {
    }
}