using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Facilities;
using System;

namespace Polymerium.App.Models;

public partial class InstanceSubpageEntryModel(Type page, PackIconLucideKind icon) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    private PackIconLucideKind _icon = icon;

    #endregion

    #region Direct

    public Type Page => page;

    #endregion
}