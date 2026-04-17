using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstanceSubpageEntryModel(Type page, Symbol icon, string label) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial Symbol Icon { get; set; } = icon;

    #endregion

    #region Direct

    public Type Page => page;

    public string Label => label;

    #endregion
}
