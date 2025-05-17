using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Modals;

public partial class PackageBulkUpdaterModal : Modal
{
    public static readonly DirectProperty<PackageBulkUpdaterModal, bool> IsFetchingProperty =
        AvaloniaProperty.RegisterDirect<PackageBulkUpdaterModal, bool>(nameof(IsFetching),
                                                                       o => o.IsFetching,
                                                                       (o, v) => o.IsFetching = v);

    public static readonly DirectProperty<PackageBulkUpdaterModal, int> FetchingTotalCountProperty =
        AvaloniaProperty.RegisterDirect<PackageBulkUpdaterModal, int>(nameof(FetchingTotalCount),
                                                                      o => o.FetchingTotalCount,
                                                                      (o, v) => o.FetchingTotalCount = v);

    public static readonly DirectProperty<PackageBulkUpdaterModal, ObservableCollection<PackageUpdaterModel>>
        UpdatesProperty =
            AvaloniaProperty
               .RegisterDirect<PackageBulkUpdaterModal, ObservableCollection<PackageUpdaterModel>>(nameof(Updates),
                    o => o.Updates,
                    (o, v) => o.Updates = v);

    private readonly CancellationTokenSource _cts = new();

    private ProfileGuard? _guard;

    public PackageBulkUpdaterModal()
    {
        InitializeComponent();
    }

    public bool IsFetching
    {
        get;
        set => SetAndRaise(IsFetchingProperty, ref field, value);
    }

    public ObservableCollection<PackageUpdaterModel> Updates
    {
        get;
        set => SetAndRaise(UpdatesProperty, ref field, value);
    } = [];


    public int FetchingTotalCount
    {
        get;
        set => SetAndRaise(FetchingTotalCountProperty, ref field, value);
    }

    public required DataService DataService { get; init; }
    public required NotificationService NotificationService { get; init; }
    public required PersistenceService PersistenceService { get; init; }

    public void SetGuard(ProfileGuard guard, SourceCache<InstancePackageModel, Profile.Rice.Entry> packages)
    {
        _guard = guard;
        _ = FetchAsync(guard, packages);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _cts.Cancel();
    }

    private async Task FetchAsync(ProfileGuard guard, SourceCache<InstancePackageModel, Profile.Rice.Entry> packages)
    {
        IsFetching = true;
        FetchingTotalCount = packages.Items.Count;
        var filter = new Filter(Kind: null,
                                Version: guard.Value.Setup.Version,
                                Loader: guard.Value.Setup.Loader is not null
                                            ? LoaderHelper.TryParse(guard.Value.Setup.Loader, out var loader)
                                                  ? loader.Identity
                                                  : null
                                            : null);
        foreach (var entry in packages.Items)
        {
            if (_cts.IsCancellationRequested)
                break;
            if (!entry.IsLocked && PackageHelper.TryParse(entry.Entry.Purl, out var result))
                if (result.Vid is not null)
                    try
                    {
                        var versions = await DataService.InspectVersionsAsync(result.Label,
                                                                              result.Namespace,
                                                                              result.Pid,
                                                                              filter);
                        var version = versions.OrderByDescending(x => x.PublishedAt).FirstOrDefault();
                        if (version != null && version.VersionId != result.Vid)
                        {
                            var package = await DataService.ResolvePackageAsync(result.Label,
                                                                                    result.Namespace,
                                                                                    result.Pid,
                                                                                    result.Vid,
                                                                                    Filter.Empty);
                            var model = new PackageUpdaterModel(entry,
                                                                package,
                                                                package.Thumbnail ?? AssetUriIndex.DIRT_IMAGE,
                                                                package.VersionId,
                                                                package.VersionName,
                                                                package.PublishedAt,
                                                                version.VersionId,
                                                                version.VersionName,
                                                                version.PublishedAt);
                            Updates.Add(model);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        NotificationService.PopMessage(ex, entry.Entry.Purl, NotificationLevel.Warning);
                    }

            FetchingTotalCount--;
        }

        IsFetching = false;
    }

    #region Commands

    [RelayCommand]
    private void Dismiss()
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
    }

    [RelayCommand]
    private void Update()
    {
        foreach (var model in Updates.Where(x => x.IsChecked))
        {
            var old = model.Entry.Entry.Purl;
            model.Entry.Version = new InstancePackageVersionModel(model.NewVersionId,
                                                                  model.NewVersionName,
                                                                  string.Join(",",
                                                                              model.Package.Requirements.AnyOfLoaders
                                                                                 .Select(LoaderHelper.ToDisplayName)),
                                                                  string.Join(",",
                                                                              model.Package.Requirements.AnyOfVersions),
                                                                  model.NewVersionTimeRaw,
                                                                  model.Package.ReleaseType);
            // 设置 Version 会同步到 Entry.Purl
            PersistenceService.AppendAction(new PersistenceService.Action(_guard!.Key,
                                                                          PersistenceService.ActionKind.EditPackage,
                                                                          old,
                                                                          model.Entry.Entry.Purl));
        }

        Dismiss();
    }

    #endregion
}