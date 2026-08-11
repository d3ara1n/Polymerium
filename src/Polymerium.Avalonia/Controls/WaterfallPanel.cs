using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace Polymerium.Avalonia.Controls;

/// <summary>
///     A Panel that arranges its children in a waterfall layout.
///     It arranges items in columns, adding each new item to the shortest column.
///     This is a non-virtualizing panel.
/// </summary>
public class WaterfallPanel : Panel
{
    /// <summary>
    ///     Defines the <see cref="ColumnWidth" /> property.
    /// </summary>
    public static readonly StyledProperty<double> ColumnWidthProperty =
        AvaloniaProperty.Register<WaterfallPanel, double>(nameof(ColumnWidth), 200.0);

    /// <summary>
    ///     Defines the <see cref="Spacing" /> property.
    /// </summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<WaterfallPanel, double>(nameof(Spacing), 10.0);

    /// <summary>
    ///     Gets or sets the width of each column.
    /// </summary>
    public double ColumnWidth
    {
        get => GetValue(ColumnWidthProperty);
        set => SetValue(ColumnWidthProperty, value);
    }

    /// <summary>
    ///     * Gets or sets the spacing between columns and rows.
    /// </summary>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>
    ///     Measures the size required for arranging the children.
    /// </summary>
    /// <param name="availableSize">The available size for the panel.</param>
    /// <returns>The desired size.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        var children = Children;
        var childCount = children.Count;
        var columnWidth = ColumnWidth;
        var spacing = Spacing;

        if (childCount == 0 || columnWidth <= 0)
        {
            return new(0, 0);
        }

        var columnCount = Math.Max(1, (int)Math.Floor((availableSize.Width + spacing) / (columnWidth + spacing)));
        var columnHeights = new double[columnCount];

        for (var i = 0; i < childCount; i++)
        {
            var child = children[i];
            child.Measure(new(columnWidth, double.PositiveInfinity));

            var shortestColumnIndex = 0;
            for (var j = 1; j < columnCount; j++)
            {
                if (columnHeights[j] < columnHeights[shortestColumnIndex])
                {
                    shortestColumnIndex = j;
                }
            }

            columnHeights[shortestColumnIndex] += child.DesiredSize.Height + spacing;
        }

        var desiredHeight = columnHeights.Max() - spacing; // Subtract last spacing
        var desiredWidth = columnCount * columnWidth + (columnCount - 1) * spacing;

        return new(Math.Max(0, desiredWidth), Math.Max(0, desiredHeight));
    }

    /// <summary>
    ///     Arranges the children within the panel.
    /// </summary>
    /// <param name="finalSize">The final size allocated to the panel.</param>
    /// <returns>The actual size used by the panel.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = Children;
        var childCount = children.Count;
        var columnWidth = ColumnWidth;
        var spacing = Spacing;

        if (childCount == 0 || columnWidth <= 0)
        {
            return finalSize;
        }

        var columnCount = Math.Max(1, (int)Math.Floor((finalSize.Width + spacing) / (columnWidth + spacing)));
        var columnHeights = new double[columnCount];

        for (var i = 0; i < childCount; i++)
        {
            var child = children[i];

            var shortestColumnIndex = 0;
            for (var j = 1; j < columnCount; j++)
            {
                if (columnHeights[j] < columnHeights[shortestColumnIndex])
                {
                    shortestColumnIndex = j;
                }
            }

            var x = shortestColumnIndex * (columnWidth + spacing);
            var y = columnHeights[shortestColumnIndex];

            var arrangeRect = new Rect(x, y, columnWidth, child.DesiredSize.Height);
            child.Arrange(arrangeRect);

            columnHeights[shortestColumnIndex] += child.DesiredSize.Height + spacing;
        }

        return finalSize;
    }
}
