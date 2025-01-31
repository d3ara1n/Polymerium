﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":loading")]
public class SkeletonControl : ContentControl
{
    public static readonly DirectProperty<SkeletonControl, bool> IsLoadingProperty =
        AvaloniaProperty.RegisterDirect<SkeletonControl, bool>(nameof(IsLoading), o => o.IsLoading,
            (o, v) => o.IsLoading = v);

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetAndRaise(IsLoadingProperty, ref _isLoading, value))
                PseudoClasses.Set(":loading", value);
        }
    }
}