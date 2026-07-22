using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

public partial class AssetIdentificationPersistModel(string path) : ModelBase
{
    #region Direct

    public string Path => path;

    public IReadOnlyList<ResourceKind> Kinds { get; } =
    [
        ResourceKind.Mod, ResourceKind.ResourcePack, ResourceKind.ShaderPack
    ];

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsInImportMode { get; set; }

    [ObservableProperty]
    public partial ResourceKind Kind { get; set; }

    #endregion
}
