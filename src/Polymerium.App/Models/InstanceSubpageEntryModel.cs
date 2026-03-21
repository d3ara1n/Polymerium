using System;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstanceSubpageEntryModel(Type page, Symbol icon) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial Symbol Icon { get; set; } = icon;

    #endregion

    #region Direct

    public Type Page => page;

    #endregion
}
