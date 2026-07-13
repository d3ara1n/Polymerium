using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;

namespace Polymerium.Avalonia.Models;

public class MultiSelectInstanceFilter : InstanceFilterBase
{
    private readonly CompositeDisposable _disposables = new();
    private readonly Func<InstanceCardModel, IEnumerable<string>> _valuesOf;

    public override string Label { get; }

    public override ReadOnlyObservableCollection<FilterOptionModel> Options { get; }

    public override IObservable<Func<InstanceCardModel, bool>> Predicate { get; }

    public override bool IsActive
    {
        get;
        protected set => SetProperty(ref field, value);
    }

    public MultiSelectInstanceFilter(
        IObservable<IChangeSet<InstanceCardModel, string>> source,
        Func<InstanceCardModel, IEnumerable<string>> valuesOf,
        string label,
        Func<string, string>? displayOf = null)
    {
        Label = label;
        _valuesOf = valuesOf;

        source.TransformMany(valuesOf, v => v)
              .Transform(v => new FilterOptionModel(v, displayOf?.Invoke(v)))
              .SortAndBind(out var options, SortExpressionComparer<FilterOptionModel>.Ascending(o => o.Label))
              .Subscribe()
              .DisposeWith(_disposables);
        Options = options;

        Predicate = Options.ToObservableChangeSet()
                           .AutoRefresh(o => o.IsSelected)
                           .Do(_ => IsActive = Options.Any(o => o.IsSelected))
                           .Select(_ => BuildPredicate());
    }

    public override void Clear()
    {
        foreach (var option in Options)
        {
            option.IsSelected = false;
        }
    }

    private Func<InstanceCardModel, bool> BuildPredicate()
    {
        var selected = Options.Where(o => o.IsSelected).Select(o => o.Value).ToHashSet();
        return selected.Count == 0
            ? _ => true
            : card => _valuesOf(card).Any(selected.Contains);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables.Dispose();
        }
    }
}
