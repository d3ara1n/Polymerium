using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

/// <summary>
///     A single directional frame consumed by the rotating skin preview: a render URL plus its
///     active state, projected into the cross-fade opacity and the indicator opacity.
/// </summary>
public partial class SkinFrameModel(Uri url) : ModelBase
{
    public Uri Url { get; } = url;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrameOpacity))]
    [NotifyPropertyChangedFor(nameof(IndicatorOpacity))]
    public partial bool IsActive { get; set; }

    public double FrameOpacity => IsActive ? 1d : 0d;

    public double IndicatorOpacity => IsActive ? 1d : 0.35d;
}
