using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

public partial class RepositoryBasicModel(string label, string name) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial IReadOnlyList<LoaderBasicModel>? Loaders { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<string>? Versions { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ResourceKind>? Kinds { get; set; }

    #endregion

    #region Direct

    public string Label => label;

    public string Name => name;

    #endregion
}
