using System;
using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstanceSubpageEntryModel(Type page, PackIconLucideKind icon) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial PackIconLucideKind Icon { get; set; } = icon;

    #endregion

    #region Direct

    public Type Page => page;

    #endregion
}
