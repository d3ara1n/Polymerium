using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;
using Trident.Core.Services.Profiles;
using Version = Trident.Abstractions.Repositories.Resources.Version;

namespace Polymerium.App.Modals;

public partial class InstancePackageModal : Modal
{
    public static readonly DirectProperty<InstancePackageModal, LazyObject?> LazyDependenciesProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageModal, LazyObject?>(nameof(LazyDependencies),
                                                                           o => o.LazyDependencies,
                                                                           (o, v) => o.LazyDependencies = v);

    public static readonly DirectProperty<InstancePackageModal, LazyObject?> LazyVersionsProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageModal, LazyObject?>(nameof(LazyVersions),
                                                                           o => o.LazyVersions,
                                                                           (o, v) => o.LazyVersions = v);

    public static readonly DirectProperty<InstancePackageModal, bool> IsFilterEnabledProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageModal, bool>(nameof(IsFilterEnabled),
                                                                    o => o.IsFilterEnabled,
                                                                    (o, v) => o.IsFilterEnabled = v);

    public static readonly DirectProperty<InstancePackageModal, InstancePackageVersionModel?>
        SelectedVersionProxyProperty =
            AvaloniaProperty
               .RegisterDirect<InstancePackageModal, InstancePackageVersionModel?>(nameof(SelectedVersionProxy),
                    o => o.SelectedVersionProxy,
                    (o, v) => o.SelectedVersionProxy = v);

    public static readonly DirectProperty<InstancePackageModal, LazyObject?> LazyDependantsProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageModal, LazyObject?>(nameof(LazyDependants),
                                                                           o => o.LazyDependants,
                                                                           (o, v) => o.LazyDependants = v);

    private string? _old;


    public InstancePackageModal() => InitializeComponent();

    public required ProfileGuard Guard { get; init; }
    public required DataService DataService { get; init; }
    public required Filter Filter { get; init; }
    public required OverlayService OverlayService { get; init; }
    public required PersistenceService PersistenceService { get; init; }
    public required SourceCache<InstancePackageModel, Profile.Rice.Entry> StageCollection { get; init; }

    public LazyObject? LazyDependencies
    {
        get;
        set => SetAndRaise(LazyDependenciesProperty, ref field, value);
    }

    public LazyObject? LazyDependants
    {
        get;
        set => SetAndRaise(LazyDependantsProperty, ref field, value);
    }

    private InstancePackageModel Model => (InstancePackageModel)DataContext!;

    public LazyObject? LazyVersions
    {
        get;
        set => SetAndRaise(LazyVersionsProperty, ref field, value);
    }

    public bool IsFilterEnabled
    {
        get;
        set => SetAndRaise(IsFilterEnabledProperty, ref field, value);
    }

    public InstancePackageVersionModel? SelectedVersionProxy
    {
        get;
        set => SetAndRaise(SelectedVersionProxyProperty, ref field, value);
    }

    private LazyObject ConstructDependencies()
    {
        var lazy = new LazyObject(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var vid = SelectedVersionProxy?.Id;
            var package = await DataService.ResolvePackageAsync(Model.Label,
                                                                Model.Namespace,
                                                                Model.ProjectId,
                                                                vid,
                                                                Filter);
            // var refCount = (uint)StageCollection.Items.Count(x => x.Version is InstancePackageVersionModel version
            //                                                    && version.Dependencies.Any(z => z.Label == Model.Label
            //                                                        && z.Namespace == Model.Namespace
            //                                                        && z.ProjectId == Model.ProjectId));
            // var strongRefCount = (uint)StageCollection.Items.Count(x => x.Version is InstancePackageVersionModel version
            //                                                          && version.Dependencies.Any(z => z.IsRequired
            //                                                              && z.Label == Model.Label
            //                                                              && z.Namespace == Model.Namespace
            //                                                              && z.ProjectId == Model.ProjectId));
            var tasks = package
                       .Dependencies.Select(async x =>
                        {
                            var count =
                                (uint)StageCollection.Items.Count(y => y.Version is InstancePackageVersionModel version
                                                                    && version.Dependencies.Any(z => z.Label == x.Label
                                                                        && z.Namespace == x.Namespace
                                                                        && z.ProjectId == x.ProjectId));
                            var found = StageCollection.Items.FirstOrDefault(y => y.Label == x.Label
                                                                              && y.Namespace == x.Namespace
                                                                              && y.ProjectId == x.ProjectId);
                            if (found is not null)
                            {
                                return new(x.Label,
                                           x.Namespace,
                                           x.ProjectId,
                                           x.VersionId,
                                           found.ProjectName,
                                           found.Thumbnail,
                                           count,
                                           x.IsRequired) { Installed = found };
                            }

                            var project = await DataService.QueryProjectAsync(x.Label, x.Namespace, x.ProjectId);
                            var thumbnail = project.Thumbnail is not null
                                                ? await DataService.GetBitmapAsync(project.Thumbnail)
                                                : AssetUriIndex.DirtImageBitmap;
                            return new InstancePackageDependencyModel(x.Label,
                                                                      x.Namespace,
                                                                      x.ProjectId,
                                                                      x.VersionId,
                                                                      project.ProjectName,
                                                                      thumbnail,
                                                                      count,
                                                                      x.IsRequired);
                        })
                       .ToArray();
            await Task.WhenAll(tasks);
            var rv = new InstancePackageDependencyCollection([.. tasks.Select(x => x.Result)]);
            return rv;
        });
        return lazy;
    }

    private LazyObject ConstructDependants()
    {
        var lazy = new LazyObject(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var dependants = await DataService.ResolvePackagesAsync(StageCollection
                                                                   .Items
                                                                   .Where(x =>
                                                                              x.Version is InstancePackageVersionModel
                                                                                  version
                                                                           && version.Dependencies.Any(y => y.Label
                                                                               == Model.Label
                                                                               && y.Namespace
                                                                               == Model.Namespace
                                                                               && y.ProjectId
                                                                               == Model.ProjectId))
                                                                   .Select(x => (x.Label, x.Namespace, x.ProjectId,
                                                                                           (string?)
                                                                                           ((InstancePackageVersionModel)
                                                                                               x.Version).Id)),
                                                                    Filter.None);
            var tasks = dependants
                       .Select(async x =>
                        {
                            var count =
                                (uint)StageCollection.Items.Count(y => y.Version is InstancePackageVersionModel version
                                                                    && version.Dependencies.Any(z =>
                                                                           z.Label == Model.Label
                                                                        && z.Namespace == Model.Namespace
                                                                        && z.ProjectId == Model.ProjectId));
                            var found = StageCollection.Items.FirstOrDefault(y => y.Label == x.Label
                                                                              && y.Namespace == x.Namespace
                                                                              && y.ProjectId == x.ProjectId);
                            var thumbnail = x.Thumbnail is not null
                                                ? await DataService.GetBitmapAsync(x.Thumbnail)
                                                : AssetUriIndex.DirtImageBitmap;
                            return new InstancePackageDependencyModel(x.Label,
                                                                      x.Namespace,
                                                                      x.ProjectId,
                                                                      x.VersionId,
                                                                      x.ProjectName,
                                                                      thumbnail,
                                                                      count,
                                                                      x.Dependencies
                                                                       .FirstOrDefault(y => y.Label == Model.Label
                                                                         && y.Namespace == Model.Namespace
                                                                         && y.ProjectId == Model.ProjectId)
                                                                      ?.IsRequired
                                                                   ?? false) { Installed = found };
                        })
                       .ToArray();
            await Task.WhenAll(tasks);
            return new InstancePackageDependencyCollection([.. tasks.Select(x => x.Result)]);
        });

        return lazy;
    }

    private LazyObject ConstructVersions()
    {
        var lazy = new LazyObject(async t =>
                                  {
                                      if (t.IsCancellationRequested)
                                      {
                                          return null;
                                      }

                                      var versions = await DataService.InspectVersionsAsync(Model.Label,
                                                         Model.Namespace,
                                                         Model.ProjectId,
                                                         IsFilterEnabled ? Filter : Filter.None);
                                      return new InstancePackageVersionCollection([
                                          .. versions.Select<Version, InstancePackageVersionModelBase>(x =>
                                              Model is
                                              {
                                                  Version:
                                                  InstancePackageVersionModel v
                                              }
                                           && v.Id == x.VersionId
                                                  ? v
                                                  : new(x.VersionId,
                                                        x.VersionName,
                                                        string.Join(",",
                                                                    x.Requirements
                                                                     .AnyOfLoaders
                                                                     .Select(LoaderHelper
                                                                                .ToDisplayName)),
                                                        string.Join(",",
                                                                    x.Requirements
                                                                     .AnyOfVersions),
                                                        x.PublishedAt,
                                                        x.ReleaseType,
                                                        x.Dependencies))
                                      ]);
                                  },
                                  _ =>
                                  {
                                      if (Model.Version is InstancePackageVersionModel v)
                                      {
                                          SelectedVersionProxy = v;
                                      }
                                  });

        return lazy;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (Design.IsDesignMode)
        {
            return;
        }

        _old = Model.Entry.Purl;
        IsFilterEnabled = true;
        LazyDependencies = ConstructDependencies();
        LazyDependants = ConstructDependants();
        AddHandler(OverlayItem.DismissRequestedEvent, DismissRequestedHandler);
    }

    private void DismissRequestedHandler(object? sender, OverlayItem.DismissRequestedEventArgs e)
    {
        if (Model.Version is InstancePackageVersionModel v)
        {
            v.IsCurrent = true;
        }
    }

    protected override async void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (Model.Entry.Purl != _old)
        {
            PersistenceService.AppendAction(new(Guard.Key,
                                                PersistenceService.ActionKind.EditPackage,
                                                _old,
                                                Model.Entry.Purl));
        }

        RemoveHandler(OverlayItem.DismissRequestedEvent, DismissRequestedHandler);
        await Guard.DisposeAsync();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsFilterEnabledProperty)
        {
            LazyVersions = ConstructVersions();
        }

        if (change.Property == SelectedVersionProxyProperty
         && change.NewValue is InstancePackageVersionModel v
         && Model.Version != v)
        {
            Model.Version = v;
        }
    }


    #region Commands

    [RelayCommand]
    private void RemoveVersion()
    {
        Model.Version = InstancePackageUnspecifiedVersionModel.Instance;
        SelectedVersionProxy = null;
    }

    [RelayCommand]
    private async Task AddTag()
    {
        var tag = await OverlayService.RequestInputAsync();
        if (string.IsNullOrEmpty(tag))
        {
            return;
        }

        Model.Tags.Add(tag);
    }

    [RelayCommand]
    private void RemoveTag(string? tag)
    {
        if (tag == null)
        {
            return;
        }

        var index = Model.Tags.IndexOf(tag);
        if (index >= 0)
        {
            Model.Tags.RemoveAt(index);
        }
    }

    [RelayCommand]
    private void ViewDependant(InstancePackageDependencyModel? model)
    {
        if (model is { Installed: { } installed })
        {
            OverlayService.PopModal(new InstancePackageModal
            {
                DataContext = installed,
                Guard = Guard,
                DataService = DataService,
                OverlayService = OverlayService,
                PersistenceService = PersistenceService,
                StageCollection = StageCollection,
                Filter = Filter
            });
        }
    }

    [RelayCommand]
    private async Task ViewDependencyAsync(InstancePackageDependencyModel? model)
    {
        // TODO: 该功能问题太多
        //  - [x] 需要未来整改整个 PackageContainer 的刷新机制便于在 InstancePackageModal.ModifyPending 中修改 Stages 集合中的项
        //  - [ ] 以及修改 InstancePackageDependencyModel 中的 Installed 状态。
        //  （后者可以通过独立的 InstancePackageDependencyModal 查看实现
        //  可以单独重构 DependencyCollection 这一块用 ExhibitDependencyModel 那一套，毕竟意义是一样的，都是对一个项目的依赖列表进行在线查询
        if (model != null)
        {
            try
            {
                var dependency = await DataService.QueryProjectAsync(model.Label, model.Namespace, model.ProjectId);
                var installed = StageCollection.Items.FirstOrDefault(x => x.Label == model.Label
                                                                       && x.Namespace == model.Namespace
                                                                       && x.ProjectId == model.ProjectId);
                var exhibit = new ExhibitModel(dependency.Label,
                                               dependency.Namespace,
                                               dependency.ProjectId,
                                               dependency.ProjectName,
                                               dependency.Summary,
                                               dependency.Thumbnail ?? AssetUriIndex.DirtImage,
                                               dependency.Author,
                                               dependency.Tags,
                                               dependency.UpdatedAt,
                                               dependency.DownloadCount,
                                               dependency.Reference)
                {
                    Installed = installed?.Entry,
                    InstalledVersionId = (installed?.Version as InstancePackageVersionModel)?.Id,
                    InstalledVersionName = (installed?.Version as InstancePackageVersionModel)?.Name,
                    PendingVersionId = null,
                    PendingVersionName = null,
                    State = installed?.IsLocked switch
                    {
                        true => ExhibitState.Locked,
                        false => ExhibitState.Editable,
                        _ => null
                    }
                };
                await ViewPackageAsync(exhibit);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                // TODO: 甩出报错
            }
        }
    }

    [RelayCommand]
    private async Task ViewPackageAsync(ExhibitModel? exhibit)
    {
        if (exhibit != null)
        {
            try
            {
                // 非 Unspecific
                if (exhibit.InstalledVersionId != null)
                {
                    var package = await DataService.ResolvePackageAsync(exhibit.Label,
                                                                        exhibit.Namespace,
                                                                        exhibit.ProjectId,
                                                                        exhibit.InstalledVersionId,
                                                                        Filter.None);
                    exhibit.InstalledVersionName = package.VersionName;
                }


                var project = await DataService.QueryProjectAsync(exhibit.Label, exhibit.Namespace, exhibit.ProjectId);
                var model = new ExhibitPackageModel(project.Label,
                                                    project.Namespace,
                                                    project.ProjectId,
                                                    project.ProjectName,
                                                    project.Author,
                                                    project.Reference,
                                                    project.Thumbnail,
                                                    project.Tags,
                                                    project.DownloadCount,
                                                    project.Summary,
                                                    project.UpdatedAt,
                                                    [.. project.Gallery.Select(x => x.Url)]);

                OverlayService.PopModal(new ExhibitPackageModal
                {
                    DataContext = model,
                    Exhibit = exhibit,
                    DataService = DataService,
                    Filter = Filter,
                    ViewPackageCommand = ViewPackageCommand,
                    ModifyPendingCallback = ModifyPending,
                    LinkExhibitCallback = LinkExhibit
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                // TODO: pop message
                // NotificationService.PopMessage(ex, "Failed to load project information", GrowlLevel.Warning);
            }
        }
    }

    #endregion

    #region ViewDependency related methods

    private ExhibitModel LinkExhibit(Project project)
    {
        var found = StageCollection.Items.FirstOrDefault(x => x.Label == project.Label
                                                           && x.Namespace == project.Namespace
                                                           && x.ProjectId == project.ProjectId);
        if (found != null)
        {
            var installed = found.Version as InstancePackageVersionModel;
            return new(project.Label,
                       project.Namespace,
                       project.ProjectId,
                       project.ProjectName,
                       project.Summary,
                       project.Thumbnail ?? AssetUriIndex.DirtImage,
                       project.Author,
                       project.Tags,
                       project.UpdatedAt,
                       project.DownloadCount,
                       project.Reference)
            {
                Installed = found.Entry,
                InstalledVersionId = installed?.Id,
                InstalledVersionName = installed?.Name,
                PendingVersionId = null,
                PendingVersionName = null,
                State = found.IsLocked ? ExhibitState.Locked : ExhibitState.Editable
            };
        }

        return new(project.Label,
                   project.Namespace,
                   project.ProjectId,
                   project.ProjectName,
                   project.Summary,
                   project.Thumbnail ?? AssetUriIndex.DirtImage,
                   project.Author,
                   project.Tags,
                   project.UpdatedAt,
                   project.DownloadCount,
                   project.Reference);
    }

    private void ModifyPending(ExhibitModel exhibit)
    {
        switch (exhibit)
        {
            case { State: ExhibitState.Adding }:
            {
                var entry = new Profile.Rice.Entry(PackageHelper.ToPurl(exhibit.Label,
                                                                        exhibit.Namespace,
                                                                        exhibit.ProjectId,
                                                                        exhibit.PendingVersionId),
                                                   true,
                                                   null,
                                                   []);
                PersistenceService.AppendAction(new(Guard.Key,
                                                    PersistenceService.ActionKind.EditPackage,
                                                    null,
                                                    entry.Purl));
                Guard.Value.Setup.Packages.Add(entry);
                exhibit.State = ExhibitState.Editable;
                exhibit.Installed = entry;
                exhibit.InstalledVersionName = exhibit.PendingVersionName;
                exhibit.InstalledVersionId = exhibit.PendingVersionId;

                // var model = new InstancePackageModel(entry,
                //                                      false,
                //                                      exhibit.Label,
                //                                      exhibit.Namespace,
                //                                      exhibit.ProjectId,
                //                                      exhibit.ProjectName,
                //                                      exhibit.PendingVersionId is not null
                //                                          ? new
                //                                              InstancePackageVersionModel(exhibit
                //                                                 .PendingVersionId, exhibit.PendingVersionName
                //                                               ?? exhibit.PendingVersionId, string.Join(",",
                //                                                  exhibit.Requirements.AnyOfLoaders
                //                                                         .Select(LoaderHelper
                //                                                                    .ToDisplayName)), string
                //                                                 .Join(",",
                //                                                       exhibit.Requirements.AnyOfVersions,
                //                                                       DateTimeOffset.MinValue,
                //                                                       ReleaseType.Release,
                //                                                       [])
                //                                          : InstancePackageUnspecifiedVersionModel.Instance,
                //                                      exhibit.Author,
                //                                      exhibit.Summary,
                //                                      exhibit.Reference,
                //                                      exhibit.Thumbnail,
                //                                      exhibit.Kind);
                // if (LazyDependencies is { Value: InstancePackageDependencyCollection deps })
                // {
                //     var dep = deps.FirstOrDefault(x => x.Label == exhibit.Label
                //                                     && x.Namespace == exhibit.Namespace
                //                                     && x.ProjectId == exhibit.ProjectId);
                //     dep.Installed = entry;
                // }

                break;
            }
            case { State: ExhibitState.Removing, Installed: not null }:
            {
                var exist = Guard.Value.Setup.Packages.FirstOrDefault(x => x.Purl == exhibit.Installed.Purl);
                if (exist != null)
                {
                    Guard.Value.Setup.Packages.Remove(exist);
                    StageCollection.Remove(exist);
                }

                PersistenceService.AppendAction(new(Guard.Key,
                                                    PersistenceService.ActionKind.EditPackage,
                                                    exhibit.Installed.Purl,
                                                    null));
                exhibit.State = null;
                exhibit.Installed = null;
                exhibit.InstalledVersionName = null;
                exhibit.InstalledVersionId = null;
                break;
            }
            case { State: ExhibitState.Modifying, Installed: not null }:
            {
                var old = exhibit.Installed.Purl;
                exhibit.Installed.Purl = PackageHelper.ToPurl(exhibit.Label,
                                                              exhibit.Namespace,
                                                              exhibit.ProjectId,
                                                              exhibit.PendingVersionId);
                PersistenceService.AppendAction(new(Guard.Key,
                                                    PersistenceService.ActionKind.EditPackage,
                                                    old,
                                                    exhibit.Installed.Purl));
                exhibit.State = ExhibitState.Editable;
                exhibit.InstalledVersionName = exhibit.PendingVersionName;
                exhibit.InstalledVersionId = exhibit.PendingVersionId;
                break;
            }
        }
    }

    #endregion
}
