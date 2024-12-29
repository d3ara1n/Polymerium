using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class RepositoryBaiscModel(
    string label,
    string name)
    : ModelBase
{
    public string Label { get; } = label;

    public string Name { get; } = name;
    [ObservableProperty] private IReadOnlyList<LoaderDisplayModel> _loaders = [];
    [ObservableProperty] private IReadOnlyList<string> _versions = [];
}