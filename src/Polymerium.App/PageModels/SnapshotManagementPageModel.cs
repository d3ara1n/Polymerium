using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.App.Facilities;
using Polymerium.App.ModalModels;
using Polymerium.App.Models;
using Polymerium.App.Services;
using TridentCore.Abstractions.Snapshots;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;

namespace Polymerium.App.PageModels;

public partial class SnapshotManagementPageModel : ViewModelBase
{
    #region Fields

    private readonly SourceCache<SnapshotItemModel, object> _source = new(x => x.Source.Id);
    private readonly CompositeDisposable _subscriptions = new();
    private readonly OverlayService _overlayService;
    private readonly NotificationService _notificationService;

    /// <inheritdoc/>
    public SnapshotManagementPageModel(IViewContext<SnapshotsModalModel.SnapshotContext> context,
                                       OverlayService overlayService,
                                       NotificationService notificationService)
    {
        _overlayService = overlayService;
        _notificationService = notificationService;
        Context = context.Parameter!;


        var textFilter = this.WhenValueChanged(x => x.SearchText)
                             .Select(BuildTextFilter);

        _source.Connect()
               .Filter(textFilter)
               .SortBy(x => x.Source.CreatedAt, SortDirection.Descending)
               .Bind(out var view)
               .Subscribe()
               .DisposeWith(_subscriptions);

        Snapshots = view;
    }

    #endregion

    #region Direct

    public SnapshotsModalModel.SnapshotContext Context { get; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
    public partial SnapshotItemModel? SelectedSnapshot { get; set; }

    [ObservableProperty]
    public partial SnapshotDetailModel? Detail { get; set; }

    #endregion

    #region Bindings

    public ReadOnlyObservableCollection<SnapshotItemModel> Snapshots { get; }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {

        try
        {
            var snapshots = Context.Handle.List();
            SnapshotItemModel? prev = null;
            var items = new List<SnapshotItemModel>();
            foreach (var info in snapshots)
            {
                var item = new SnapshotItemModel { Source = info, Previous = prev };
                items.Add(item);
                prev = item;
            }

            _source.Edit(updater => updater.AddOrUpdate(items));
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, "Failed to load snapshots");
        }

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        _subscriptions.Dispose();
        _source.Dispose();
        return Task.CompletedTask;
    }

    #endregion

    #region Callbacks

    partial void OnSelectedSnapshotChanged(SnapshotItemModel? value)
    {
        if (value != null)
        {
            var info = value.Source;

            SnapshotDiffModel? diff = null;
            if (value.Previous != null)
            {
                try
                {
                    diff = ComputeDiff(value.Source, value.Previous.Source);
                }
                catch (Exception ex)
                {
                    _notificationService.PopMessage(ex, "Failed to compute diff");
                }
            }

            Detail = new()
            {
                Label = info.Label,
                Remark = info.Remark,
                CreatedAt = info.CreatedAt,
                GameVersion = info.Metadata.Version,
                Loader = LoaderHelper.ToDisplayLabel(info.Metadata.Loader),
                PackageCount = info.PackageCount,
                FileCount = info.FileCount,
                TotalSize = info.TotalSize,
                Diff = diff,
            };
        }
        else
        {
            Detail = null;
        }
    }

    #endregion

    #region Commands

    private bool CanAct() => SelectedSnapshot != null;

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task DeleteAsync()
    {
        if (SelectedSnapshot is not { } target)
            return;

        var label = target.DisplayLabel;
        var confirmed = await _overlayService.RequestConfirmationAsync(
            $"确定要删除快照「{label}」吗？此操作不可撤销。",
            "删除快照");
        if (!confirmed)
            return;

        try
        {
            Context.Handle.Delete(target.Source.Id);

            var deletedPrevious = target.Previous;
            _source.Edit(updater =>
            {
                updater.Remove(target.Source.Id);

                // Fix the chain: find items whose Previous was the deleted item
                var needsFix = updater.Items
                                      .Where(x => x.Previous == target)
                                      .ToList();
                foreach (var item in needsFix)
                {
                    updater.AddOrUpdate(item.WithPrevious(deletedPrevious));
                }
            });

            SelectedSnapshot = null;
            _notificationService.PopMessage($"快照「{label}」已删除", "删除成功", GrowlLevel.Success);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, "删除快照失败");
        }
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task RestoreAsync()
    {
        if (SelectedSnapshot is not { } target)
            return;

        var label = target.DisplayLabel;
        var confirmed = await _overlayService.RequestConfirmationAsync(
            $"还原到快照「{label}」将覆盖当前的文件布局，确定继续吗？",
            "还原快照");
        if (!confirmed)
            return;

        // TODO: implement restore
        _notificationService.PopMessage("还原功能尚未实现", "快照还原", GrowlLevel.Warning);
    }

    #endregion

    #region Private Methods

    private SnapshotDiffModel ComputeDiff(SnapshotInfo current, SnapshotInfo previous)
    {
        var currentRefs = Context.Handle.GetReferences(current.Id);
        var previousRefs = Context.Handle.GetReferences(previous.Id);

        var currentPaths = new HashSet<string>(currentRefs.Select(x => x.RelativePath), StringComparer.OrdinalIgnoreCase);
        var previousPaths = new HashSet<string>(previousRefs.Select(x => x.RelativePath), StringComparer.OrdinalIgnoreCase);

        var currentPurls = new HashSet<string>(current.Metadata.Packages.Select(x => x.Purl), StringComparer.OrdinalIgnoreCase);
        var previousPurls = new HashSet<string>(previous.Metadata.Packages.Select(x => x.Purl), StringComparer.OrdinalIgnoreCase);

        return new SnapshotDiffModel
        {
            FilesAdded = currentPaths.Count - currentPaths.Intersect(previousPaths).Count(),
            FilesRemoved = previousPaths.Count - previousPaths.Intersect(currentPaths).Count(),
            PackagesAdded = currentPurls.Count - currentPurls.Intersect(previousPurls).Count(),
            PackagesRemoved = previousPurls.Count - previousPurls.Intersect(currentPurls).Count(),
        };
    }

    private static Func<SnapshotItemModel, bool> BuildTextFilter(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return _ => true;

        var lower = text.Trim();
        return x => (x.Source.Label ?? string.Empty).Contains(lower, StringComparison.OrdinalIgnoreCase)
                 || (x.Source.Remark ?? string.Empty).Contains(lower, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatDiff(int delta) => delta switch
    {
        > 0 => $"+{delta}",
        < 0 => delta.ToString(),
        _ => "0"
    };

    #endregion
}
