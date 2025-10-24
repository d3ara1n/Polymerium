using Avalonia;
using Avalonia.Controls;
using System;
using System.Linq;

namespace Polymerium.App.Controls;

/// <summary>
/// A Panel that arranges its children in a waterfall layout.
/// It arranges items in columns, adding each new item to the shortest column.
/// This is a non-virtualizing panel.
/// </summary>
public class WaterfallPanel : Panel
{
    /// <summary>
    /// Defines the <see cref="ColumnWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ColumnWidthProperty =
        AvaloniaProperty.Register<WaterfallPanel, double>(nameof(ColumnWidth), 200.0);

    /// <summary>
    /// Gets or sets the width of each column.
    /// </summary>
    public double ColumnWidth
    {
        get => GetValue(ColumnWidthProperty);
        set => SetValue(ColumnWidthProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="Spacing"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<WaterfallPanel, double>(nameof(Spacing), 10.0);

    /// <summary>
    /// * Gets or sets the spacing between columns and rows.
    /// </summary>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>
    /// Measures the size required for arranging the children.
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
            return new Size(0, 0);
        }

        // Calculate the number of columns that can fit in the available width.
        var columnCount = Math.Max(1, (int)Math.Floor((availableSize.Width + spacing) / (columnWidth + spacing)));
        var columnHeights = new double[columnCount];

        // Measure each child and distribute them into columns.
        for (var i = 0; i < childCount; i++)
        {
            var child = children[i];
            child.Measure(new Size(columnWidth, double.PositiveInfinity));

            // Find the shortest column.
            var shortestColumnIndex = 0;
            for (var j = 1; j < columnCount; j++)
            {
                if (columnHeights[j] < columnHeights[shortestColumnIndex])
                {
                    shortestColumnIndex = j;
                }
            }

            // Add the child's height to the shortest column.
            columnHeights[shortestColumnIndex] += child.DesiredSize.Height + spacing;
        }

        // The desired height of the panel is the height of the tallest column.
        var desiredHeight = columnHeights.Max() - spacing; // Subtract last spacing
        var desiredWidth = columnCount * columnWidth + (columnCount - 1) * spacing;

        return new Size(Math.Max(0, desiredWidth), Math.Max(0, desiredHeight));
    }

    /// <summary>
    /// Arranges the children within the panel.
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

        // Calculate the number of columns.
        var columnCount = Math.Max(1, (int)Math.Floor((finalSize.Width + spacing) / (columnWidth + spacing)));
        var columnHeights = new double[columnCount];

        // Arrange each child in its final position.
        for (var i = 0; i < childCount; i++)
        {
            var child = children[i];

            // Find the shortest column.
            var shortestColumnIndex = 0;
            for (var j = 1; j < columnCount; j++)
            {
                if (columnHeights[j] < columnHeights[shortestColumnIndex])
                {
                    shortestColumnIndex = j;
                }
            }

            // Calculate the position for the child.
            var x = shortestColumnIndex * (columnWidth + spacing);
            var y = columnHeights[shortestColumnIndex];

            var arrangeRect = new Rect(x, y, columnWidth, child.DesiredSize.Height);
            child.Arrange(arrangeRect);

            // Update the column height.
            columnHeights[shortestColumnIndex] += child.DesiredSize.Height + spacing;
        }

        return finalSize;
    }
}
