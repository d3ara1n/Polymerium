using System;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public partial class InstanceSubpageEntryModel(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    Type page,
    Symbol icon,
    string label) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial Symbol Icon { get; set; } = icon;

    #endregion

    #region Direct

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type Page => page;

    public string Label => label;

    #endregion
}
