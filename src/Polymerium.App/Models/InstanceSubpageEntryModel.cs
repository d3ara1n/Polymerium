using System;
using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstanceSubpageEntryModel(Type page, PackIconLucideKind icon) : ModelBase
{
    #region Direct

    public Type Page => page;

    #endregion

    #region Reactive

    [ObservableProperty]
    private PackIconLucideKind _icon = icon;

    #endregion
}