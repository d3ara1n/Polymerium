using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Polymerium.App.Models;

namespace Polymerium.App.Controls;

public class DiffView : TemplatedControl
{
    public const double GUTTER_WIDTH = 48.0;
    public const double SEPARATOR_WIDTH = 1.0;
    public const double MEASURE_FONT_SIZE = 13.0;
    public const double LINE_HEIGHT = 22.0;

    private static readonly Typeface MonospaceTypeface =
        new(new FontFamily("Cascadia Code, Consolas, Courier New, monospace"));

    public static readonly StyledProperty<string?> LeftTextProperty =
        AvaloniaProperty.Register<DiffView, string?>(nameof(LeftText));

    public static readonly StyledProperty<string?> RightTextProperty =
        AvaloniaProperty.Register<DiffView, string?>(nameof(RightText));

    public static readonly StyledProperty<IReadOnlyList<DiffLineModel>?> LinesProperty =
        AvaloniaProperty.Register<DiffView, IReadOnlyList<DiffLineModel>?>(nameof(Lines));

    public static readonly StyledProperty<double> HorizontalOffsetProperty =
        AvaloniaProperty.Register<DiffView, double>(nameof(HorizontalOffset));

    public static readonly StyledProperty<double> ContentWidthProperty =
        AvaloniaProperty.Register<DiffView, double>(nameof(ContentWidth));

    public static readonly StyledProperty<int> LeftLineCountProperty =
        AvaloniaProperty.Register<DiffView, int>(nameof(LeftLineCount));

    public static readonly StyledProperty<int> RightLineCountProperty =
        AvaloniaProperty.Register<DiffView, int>(nameof(RightLineCount));

    public static readonly StyledProperty<int> TotalLineCountProperty =
        AvaloniaProperty.Register<DiffView, int>(nameof(TotalLineCount));

    public static readonly StyledProperty<bool> LeftHasDifferenceProperty =
        AvaloniaProperty.Register<DiffView, bool>(nameof(LeftHasDifference));

    public static readonly StyledProperty<bool> RightHasDifferenceProperty =
        AvaloniaProperty.Register<DiffView, bool>(nameof(RightHasDifference));

    public bool LeftHasDifference
    {
        get => GetValue(LeftHasDifferenceProperty);
        private set => SetValue(LeftHasDifferenceProperty, value);
    }

    public bool RightHasDifference
    {
        get => GetValue(RightHasDifferenceProperty);
        private set => SetValue(RightHasDifferenceProperty, value);
    }

    public int LeftLineCount
    {
        get => GetValue(LeftLineCountProperty);
        private set => SetValue(LeftLineCountProperty, value);
    }

    public int RightLineCount
    {
        get => GetValue(RightLineCountProperty);
        private set => SetValue(RightLineCountProperty, value);
    }

    public int TotalLineCount
    {
        get => GetValue(TotalLineCountProperty);
        private set => SetValue(TotalLineCountProperty, value);
    }

    public string? LeftText
    {
        get => GetValue(LeftTextProperty);
        set => SetValue(LeftTextProperty, value);
    }

    public string? RightText
    {
        get => GetValue(RightTextProperty);
        set => SetValue(RightTextProperty, value);
    }

    public IReadOnlyList<DiffLineModel>? Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    public double HorizontalOffset
    {
        get => GetValue(HorizontalOffsetProperty);
        private set => SetValue(HorizontalOffsetProperty, value);
    }

    public double ContentWidth
    {
        get => GetValue(ContentWidthProperty);
        private set => SetValue(ContentWidthProperty, value);
    }

    private ScrollBar? _hScrollBar;
    private ScrollViewer? _scrollViewer;
    private double _maxContentWidth;
    private double _lastViewportWidth;
    private double _lastMaxContentWidth;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LeftTextProperty || change.Property == RightTextProperty)
        {
            UpdateDiff();
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _hScrollBar = e.NameScope.Find<ScrollBar>("PART_HScrollBar");

        if (_hScrollBar != null)
            _hScrollBar.ValueChanged += OnHScrollBarValueChanged;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        UpdateScrollBarMaximum(force: false);
        return result;
    }

    private void OnHScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e) =>
        SetCurrentValue(HorizontalOffsetProperty, -e.NewValue);

    private void UpdateDiff()
    {
        if (LeftText is null || RightText is null)
        {
            // 存在 null，等文本全部准备完毕时才触发（至少也要赋值为 string.Empty）
            return;
        }

        var diff = SideBySideDiffBuilder.Instance.BuildDiffModel(RightText, LeftText);

        var leftLines = diff.NewText.Lines;
        var rightLines = diff.OldText.Lines;
        var count = leftLines.Count;
        var lines = new List<DiffLineModel>(count);

        LeftLineCount = leftLines.Count(x => x.Position != null);
        RightLineCount = rightLines.Count(x => x.Position != null);
        TotalLineCount = count;
        LeftHasDifference = diff.OldText.HasDifferences;
        RightHasDifference = diff.NewText.HasDifferences;

        double maxTextWidth = 0;

        for (var i = 0; i < count; i++)
        {
            var left = leftLines[i];
            var right = rightLines[i];

            var leftText = left.Text ?? string.Empty;
            var rightText = right.Text ?? string.Empty;

            if (!string.IsNullOrEmpty(leftText))
            {
                var ft = new FormattedText(leftText,
                                           CultureInfo.CurrentCulture,
                                           FlowDirection.LeftToRight,
                                           MonospaceTypeface,
                                           MEASURE_FONT_SIZE,
                                           null);
                maxTextWidth = Math.Max(maxTextWidth, ft.WidthIncludingTrailingWhitespace);
            }

            if (!string.IsNullOrEmpty(rightText))
            {
                var ft = new FormattedText(rightText,
                                           CultureInfo.CurrentCulture,
                                           FlowDirection.LeftToRight,
                                           MonospaceTypeface,
                                           MEASURE_FONT_SIZE,
                                           null);
                maxTextWidth = Math.Max(maxTextWidth, ft.WidthIncludingTrailingWhitespace);
            }

            lines.Add(new DiffLineModel
            {
                LeftText = leftText,
                RightText = rightText,
                LeftLineNumber = left.Position?.ToString() ?? string.Empty,
                RightLineNumber = right.Position?.ToString() ?? string.Empty,
                LeftKind = ToKind(left.Type),
                RightKind = ToKind(right.Type),
            });
        }

        _maxContentWidth = maxTextWidth + 16;
        SetCurrentValue(ContentWidthProperty, _maxContentWidth);
        UpdateScrollBarMaximum(force: true);
        SetCurrentValue(HorizontalOffsetProperty, 0.0);

        if (_hScrollBar != null)
            _hScrollBar.Value = 0;

        SetCurrentValue(LinesProperty, lines);
    }

    private void UpdateScrollBarMaximum(bool force = false)
    {
        if (_scrollViewer == null || _hScrollBar == null)
            return;

        var viewportWidth = _scrollViewer.Viewport.Width;

        if (!force
         && Math.Abs(viewportWidth - _lastViewportWidth) < 0.5
         && Math.Abs(_maxContentWidth - _lastMaxContentWidth) < 0.5)
            return;

        _lastViewportWidth = viewportWidth;
        _lastMaxContentWidth = _maxContentWidth;

        var contentColumnWidth = Math.Max(0, (viewportWidth - GUTTER_WIDTH * 2 - SEPARATOR_WIDTH) / 2);

        _hScrollBar.Maximum = Math.Max(0, _maxContentWidth - contentColumnWidth);
        _hScrollBar.ViewportSize = contentColumnWidth;
        _hScrollBar.SmallChange = 40;
        _hScrollBar.LargeChange = contentColumnWidth;
        _hScrollBar.IsVisible = _hScrollBar.Maximum > 0;
    }

    private static DiffLineKind ToKind(ChangeType type) =>
        type switch
        {
            ChangeType.Unchanged => DiffLineKind.Unchanged,
            ChangeType.Deleted => DiffLineKind.Removed,
            ChangeType.Inserted => DiffLineKind.Added,
            ChangeType.Modified => DiffLineKind.Modified,
            ChangeType.Imaginary => DiffLineKind.Empty,
            _ => DiffLineKind.Unchanged,
        };
}
