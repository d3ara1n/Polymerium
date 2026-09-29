using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public sealed partial class InstanceGroupModel : ModelBase, IDisposable
{
    private readonly IDisposable _subscription;

    public InstanceGroupModel(IGroup<InstanceCardModel, string, string> group, IComparer<InstanceCardModel> comparer)
    {
        _subscription = group.Cache.Connect().SortAndBind(out var cards, comparer).Subscribe();
        Cards = cards;
        Label = group.Key;
    }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    public string Label { get; }
    public ReadOnlyObservableCollection<InstanceCardModel> Cards { get; }

    void IDisposable.Dispose() => _subscription.Dispose();
}
