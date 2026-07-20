using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;
using Res = Polymerium.Avalonia.Properties.Resources;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Dialogs;

public partial class PackageSelectorDialog : Dialog
{
    public enum SelectionIntent
    {
        Remove,
        Enable,
        Disable,
    }

    public static readonly DirectProperty<
        PackageSelectorDialog,
        ReadOnlyObservableCollection<SelectablePackageModel>?
    > ViewProperty = AvaloniaProperty.RegisterDirect<
        PackageSelectorDialog,
        ReadOnlyObservableCollection<SelectablePackageModel>?
    >(nameof(View), o => o.View, (o, v) => o.View = v);

    public static readonly DirectProperty<PackageSelectorDialog, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<PackageSelectorDialog, string>(
            nameof(FilterText), o => o.FilterText, (o, v) => o.FilterText = v);

    public static readonly DirectProperty<PackageSelectorDialog, string> SelectionSummaryProperty =
        AvaloniaProperty.RegisterDirect<PackageSelectorDialog, string>(
            nameof(SelectionSummary), o => o.SelectionSummary);

    private readonly SourceCache<SelectablePackageModel, string> _items = new(x => x.Source.Entry.Pref);

    private readonly CompositeDisposable _subscriptions = new();

    public PackageSelectorDialog()
    {
        InitializeComponent();

        var filterText = this.GetObservable(FilterTextProperty).Select(BuildFilter);
        _items
           .Connect()
           .Filter(filterText)
           .Bind(out var view)
           .Subscribe()
           .DisposeWith(_subscriptions);
        View = view;

        // 任一项勾选变化 → 重算 Result 与计数。Result 的变更同时驱动主按钮可用性（ValidateResult）。
        _items
           .Connect()
           .AutoRefresh(x => x.IsSelected)
           .Subscribe(_ => RecomputeSelection())
           .DisposeWith(_subscriptions);
    }

    public ReadOnlyObservableCollection<SelectablePackageModel>? View
    {
        get;
        set => SetAndRaise(ViewProperty, ref field, value);
    }

    public string FilterText
    {
        get;
        set => SetAndRaise(FilterTextProperty, ref field, value);
    } = string.Empty;

    public string SelectionSummary
    {
        get;
        private set => SetAndRaise(SelectionSummaryProperty, ref field, value);
    } = string.Empty;

    // 纯 CLR：caller 在 SetItems / 显示前设置一次。setter 把意图映射到标题与主按钮文案。
    private SelectionIntent _intent = SelectionIntent.Remove;

    public SelectionIntent Intent
    {
        get => _intent;
        set
        {
            _intent = value;
            ApplyIntent();
        }
    }

    // 圈选维度快照（SetItems 时构建）——注册为 DirectProperty 以保证绑定在任意时序下都能收到值
    public static readonly DirectProperty<PackageSelectorDialog, IReadOnlyList<ResourceKind>> TypesProperty =
        AvaloniaProperty.RegisterDirect<PackageSelectorDialog, IReadOnlyList<ResourceKind>>(
            nameof(Types), o => o.Types, (o, v) => o.Types = v);

    public static readonly DirectProperty<PackageSelectorDialog, IReadOnlyList<string>> AuthorsProperty =
        AvaloniaProperty.RegisterDirect<PackageSelectorDialog, IReadOnlyList<string>>(
            nameof(Authors), o => o.Authors, (o, v) => o.Authors = v);

    public static readonly DirectProperty<PackageSelectorDialog, IReadOnlyList<string>> TagsProperty =
        AvaloniaProperty.RegisterDirect<PackageSelectorDialog, IReadOnlyList<string>>(
            nameof(Tags), o => o.Tags, (o, v) => o.Tags = v);

    private IReadOnlyList<ResourceKind> _types = [];
    private IReadOnlyList<string> _authors = [];
    private IReadOnlyList<string> _tags = [];

    public IReadOnlyList<ResourceKind> Types
    {
        get => _types;
        private set => SetAndRaise(TypesProperty, ref _types, value);
    }

    public IReadOnlyList<string> Authors
    {
        get => _authors;
        private set => SetAndRaise(AuthorsProperty, ref _authors, value);
    }

    public IReadOnlyList<string> Tags
    {
        get => _tags;
        private set => SetAndRaise(TagsProperty, ref _tags, value);
    }

    private int _totalCount;

    public void SetItems(IReadOnlyList<SelectablePackageModel> items)
    {
        _items.Clear();
        _items.AddOrUpdate(items);
        _totalCount = items.Count;
        Types = items
               .Where(x => x.Kind is not null)
               .Select(x => x.Kind!.Value)
               .Distinct()
               .OrderBy(x => x)
               .ToList();
        Authors = items
                 .Where(x => !string.IsNullOrEmpty(x.Author))
                 .Select(x => x.Author!)
                 .Distinct()
                 .OrderBy(x => x)
                 .ToList();
        Tags = items.SelectMany(x => x.Tags).Distinct().OrderBy(x => x).ToList();
        RecomputeSelection();
    }

    // 作用域划分：搜索条是分界线。
    // 局部（作用于当前过滤后的可见视图）：SelectAllVisible / ClearVisible——搜索只是帮用户定位项。
    // 全局（作用于完整候选集）：ClearAll / SelectByType / SelectByAuthor / SelectByTag。
    [RelayCommand]
    private void SelectAllVisible()
    {
        foreach (var item in View ?? Enumerable.Empty<SelectablePackageModel>())
            item.IsSelected = true;
    }

    [RelayCommand]
    private void ClearVisible()
    {
        foreach (var item in View ?? Enumerable.Empty<SelectablePackageModel>())
            item.IsSelected = false;
    }

    [RelayCommand]
    private void ClearAll()
    {
        foreach (var item in _items.Items)
            item.IsSelected = false;
    }

    [RelayCommand]
    private void SelectByType(ResourceKind kind)
    {
        foreach (var item in _items.Items)
            if (item.Kind == kind)
                item.IsSelected = true;
    }

    [RelayCommand]
    private void SelectByAuthor(string author)
    {
        foreach (var item in _items.Items)
            if (item.Author == author)
                item.IsSelected = true;
    }

    [RelayCommand]
    private void SelectByTag(string tag)
    {
        foreach (var item in _items.Items)
            if (item.Tags.Contains(tag))
                item.IsSelected = true;
    }

    protected override bool ValidateResult(object? result) =>
        result is IReadOnlyList<SelectablePackageModel> { Count: > 0 };

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _subscriptions.Dispose();
        base.OnUnloaded(e);
    }

    private void RecomputeSelection()
    {
        var selected = _items.Items.Where(x => x.IsSelected).ToList();
        Result = selected;
        SelectionSummary = Res.PackageSelectorDialog_SelectedCountText
                              .Replace("{0}", selected.Count.ToString())
                              .Replace("{1}", _totalCount.ToString());
    }

    private void ApplyIntent()
    {
        (Title, PrimaryText) = _intent switch
        {
            SelectionIntent.Remove => (Res.PackageSelectorDialog_RemoveTitle,
                                       Res.PackageSelectorDialog_RemoveText),
            SelectionIntent.Enable => (Res.PackageSelectorDialog_EnableTitle,
                                       Res.PackageSelectorDialog_EnableText),
            SelectionIntent.Disable => (Res.PackageSelectorDialog_DisableTitle,
                                        Res.PackageSelectorDialog_DisableText),
            _ => (Title, PrimaryText),
        };
    }

    private static Func<SelectablePackageModel, bool> BuildFilter(string? filter) =>
        x => string.IsNullOrEmpty(filter)
          || x.Label.Contains(filter, StringComparison.OrdinalIgnoreCase)
          || (x.Author?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
          || x.Source.Entry.Pref.Contains(filter, StringComparison.OrdinalIgnoreCase);
}
