using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Primitives;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Polymerium.App.Models;

namespace Polymerium.App.Controls;

public class DiffView : TemplatedControl
{
    public static readonly StyledProperty<string?> OldTextProperty =
        AvaloniaProperty.Register<DiffView, string?>(nameof(OldText));

    public static readonly StyledProperty<string?> NewTextProperty =
        AvaloniaProperty.Register<DiffView, string?>(nameof(NewText));

    public static readonly StyledProperty<IReadOnlyList<DiffLineModel>?> LinesProperty =
        AvaloniaProperty.Register<DiffView, IReadOnlyList<DiffLineModel>?>(nameof(Lines));

    public string? OldText
    {
        get => GetValue(OldTextProperty);
        set => SetValue(OldTextProperty, value);
    }

    public string? NewText
    {
        get => GetValue(NewTextProperty);
        set => SetValue(NewTextProperty, value);
    }

    public IReadOnlyList<DiffLineModel>? Lines
    {
        get => GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }

    static DiffView()
    {
        OldTextProperty.Changed.AddClassHandler<DiffView>(OnDiffInputChanged);
        NewTextProperty.Changed.AddClassHandler<DiffView>(OnDiffInputChanged);
    }

    private static void OnDiffInputChanged(DiffView sender, AvaloniaPropertyChangedEventArgs e) =>
        sender.UpdateDiff();

    private void UpdateDiff()
    {
        var diff = SideBySideDiffBuilder.Instance.BuildDiffModel(
            OldText ?? string.Empty,
            NewText ?? string.Empty
        );

        var oldLines = diff.OldText.Lines;
        var newLines = diff.NewText.Lines;
        var count = oldLines.Count;
        var lines = new List<DiffLineModel>(count);

        for (var i = 0; i < count; i++)
        {
            var left = oldLines[i];
            var right = newLines[i];

            lines.Add(
                new DiffLineModel
                {
                    LeftText = left.Text ?? string.Empty,
                    RightText = right.Text ?? string.Empty,
                    LeftLineNumber = left.Position?.ToString() ?? string.Empty,
                    RightLineNumber = right.Position?.ToString() ?? string.Empty,
                    LeftKind = ToKind(left.Type),
                    RightKind = ToKind(right.Type),
                }
            );
        }

        SetCurrentValue(LinesProperty, lines);
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
