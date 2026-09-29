using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Polymerium.Avalonia.Facilities;
using System.Reactive.Disposables;

namespace Polymerium.Avalonia.Models;

public sealed partial class InstanceGroupModel : ModelBase, IDisposable
{
    private readonly CompositeDisposable _subscriptions;

    public InstanceGroupModel(
        IGroup<InstanceCardModel, string, InstanceGroupKey> group,
        IComparer<InstanceCardModel> comparer)
    {
        var sorted = group.Cache.Connect().SortAndBind(out var cards, comparer);
        Cards = cards;
        Key = group.Key;
        Label = group.Key.Label;

        var sortedSubscription = sorted.Subscribe();
        var changeSubscription = group.Cache.Connect().Subscribe(_ => UpdateFirstCard());
        _subscriptions = new CompositeDisposable(sortedSubscription, changeSubscription);
        UpdateFirstCard();
    }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;

    [ObservableProperty]
    public partial InstanceCardModel? FirstCard { get; private set; }

    [ObservableProperty]
    public partial int FirstCardVersion { get; private set; }

    public InstanceGroupKey Key { get; }

    public LocalizedLabelBase Label { get; }
    public ReadOnlyObservableCollection<InstanceCardModel> Cards { get; }

    private void UpdateFirstCard()
    {
        FirstCard = Cards.FirstOrDefault();
        FirstCardVersion++;
    }

    void IDisposable.Dispose()
    {
        _subscriptions.Dispose();
    }
}
