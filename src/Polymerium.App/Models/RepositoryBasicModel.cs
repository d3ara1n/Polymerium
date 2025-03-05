using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class RepositoryBasicModel(string label, string name) : ModelBase
{
    [ObservableProperty]
    private IReadOnlyList<LoaderBasicModel> _loaders = [];

    [ObservableProperty]
    private IReadOnlyList<string> _versions = [];

    public string Label { get; } = label;

    public string Name { get; } = name;
}