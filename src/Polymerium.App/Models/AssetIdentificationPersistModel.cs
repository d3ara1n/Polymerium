using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

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
