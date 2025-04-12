using Avalonia;
using Avalonia.Controls;

namespace Huskui.Avalonia.Controls;

public class FlexWrapPanel : Panel
{
    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<FlexWrapPanel, double>(nameof(RowSpacing));

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<FlexWrapPanel, double>(nameof(ColumnSpacing));

    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var totalWidth = availableSize.Width;
        var totalHeight = 0d;
        var row = 0;
        var start = 0;
        while (start < Children.Count)
        {
            var count = SeparateLines(totalWidth, start);
            var sumCount = 0;
            var sumWidth = 0d;
            for (var i = start; i < start + count; i++)
            {
                var child = Children[i];
                if (!double.IsNaN(child.Width))
                {
                    sumWidth += child.Width;
                    sumCount++;
                }
            }

            var avgWidth = (totalWidth - ColumnSpacing * (count - 1) - sumWidth) / (count - sumCount);

            var rowMaxHeight = 0d;
            for (var i = start; i < start + count; i++)
            {
                var child = Children[i];
                var width = !double.IsNaN(child.Width) ? child.Width : avgWidth;
                if (!double.IsNegativeInfinity(child.MinWidth) && width < child.MinWidth)
                    width = child.MinWidth;
                child.Measure(new Size(width, availableSize.Height));
                rowMaxHeight = double.Max(rowMaxHeight, child.DesiredSize.Height);
            }

            totalHeight += rowMaxHeight;

            start += count;
            row++;
        }

        return new Size(totalWidth, double.Max(totalHeight + RowSpacing * (row - 1), 0d));
    }

    protected override Size ArrangeOverride(Size final)
    {
        var totalWidth = final.Width;
        var totalHeight = 0d;
        var row = 0;
        var start = 0;
        while (start < Children.Count)
        {
            var count = SeparateLines(totalWidth, start);
            var sumCount = 0;
            var sumWidth = 0d;
            var maxHeight = 0d;
            for (var i = start; i < start + count; i++)
            {
                var child = Children[i];
                if (!double.IsNaN(child.Width))
                {
                    sumWidth += child.Width;
                    sumCount++;
                }

                maxHeight = double.Max(maxHeight, child.DesiredSize.Height);
            }

            var avgWidth = (totalWidth - ColumnSpacing * (count - 1) - sumWidth) / (count - sumCount);
            for (var i = start; i < start + count; i++)
            {
                var child = Children[i];
                if (!double.IsNegativeInfinity(child.MinWidth) && avgWidth < child.MinWidth)
                {
                    sumWidth += child.MinWidth;
                    sumCount++;
                }
            }

            avgWidth = (totalWidth - ColumnSpacing * (count - 1) - sumWidth) / (count - sumCount);

            var usedWidth = 0d;
            for (var i = start; i < start + count; i++)
            {
                var child = Children[i];
                var width = !double.IsNaN(child.Width) ? child.Width : avgWidth;
                if (!double.IsPositiveInfinity(child.MaxWidth) && width > child.MaxWidth)
                    width = child.MaxWidth;
                if (!double.IsNegativeInfinity(child.MinWidth) && width < child.MinWidth)
                    width = child.MinWidth;
                if (double.IsInfinity(width))
                    width = child.DesiredSize.Width;

                var height = !double.IsNaN(child.Height) ? child.Height : maxHeight;
                if (!double.IsNegativeInfinity(child.MinHeight) && height < child.MinHeight)
                    height = child.MinHeight;
                if (!double.IsPositiveInfinity(child.MaxHeight) && height > child.MaxHeight)
                    height = child.MaxHeight;
                if (double.IsInfinity(height))
                    height = child.DesiredSize.Height;

                // TODO: 当 usedWidth = x.5 且 width 也是 x.5 时，最后一颗像素会被裁剪掉
                // TODO: 当 HorizontalAlignment 不为 Stretch 时，依旧用同一个 count，但取每个 child 的 MinWidth

                child.Arrange(new Rect(usedWidth, totalHeight + RowSpacing * row, width, height));
                usedWidth += width + ColumnSpacing;
            }

            totalHeight += maxHeight;

            start += count;
            row++;
        }

        return new Size(totalWidth, double.Max(totalHeight + RowSpacing * (row - 1), 0d));
    }

    private int SeparateLines(double width, int start)
    {
        // 按最大尺寸计算，算出来的其实是最少可容纳量
        var maxCount = 0;
        // 算出来的其实是最多可容纳量
        var minCount = 0;
        var minWidth = 0d;
        var maxWidth = 0d;
        while (start < Children.Count)
        {
            var child = Children[start];
            var min = !double.IsNaN(child.Width) ? child.Width : child.MinWidth;
            var max = !double.IsNaN(child.Width) ? child.Width : child.MaxWidth;
            if (double.IsNegativeInfinity(min))
                min = 0d;
            if (double.IsPositiveInfinity(max))
                max = width - maxWidth;
            if (max > double.Epsilon && width - (maxWidth + max + ColumnSpacing * maxCount) > double.Epsilon)
            {
                maxWidth += max;
                maxCount++;
            }

            if (min > double.Epsilon && width - (minWidth + ColumnSpacing * minCount) > double.Epsilon)
            {
                minWidth += min;
                minCount++;
            }
            else
            {
                break;
            }

            start++;
        }

        return (maxCount + minCount + 1) / 2;
    }
}