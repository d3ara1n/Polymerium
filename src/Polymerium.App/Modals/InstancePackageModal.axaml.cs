using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.Repositories;
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
    public required IReadOnlyList<InstancePackageModel> Collection { get; init; }

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

    private InstancePackageInfoModel Model => (InstancePackageInfoModel)DataContext!;

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
            var tasks = package
                       .Dependencies.Select(async x =>
                        {
                            var count = (uint)Collection.Count(y => y.Info is
                                                                        {
                                                                            Version: InstancePackageVersionModel version
                                                                        }
                                                                 && version.Dependencies.Any(z => z.Label == x.Label
                                                                     && z.Namespace == x.Namespace
                                                                     && z.ProjectId == x.ProjectId));
                            var found = Collection.FirstOrDefault(y => y.Info?.Label == x.Label
                                                                    && y.Info?.Namespace == x.Namespace
                                                                    && y.Info?.ProjectId == x.ProjectId);
                            if (found is not null)
                            {
                                return new(x.Label,
                                           x.Namespace,
                                           x.ProjectId,
                                           x.VersionId,
                                           found.Info!.ProjectName,
                                           found.Info!.Thumbnail,
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

            var dependants = await DataService.ResolvePackagesAsync(Collection
                                                                   .Where(x => x.Info is
                                                                               {
                                                                                   Version:
                                                                                   InstancePackageVersionModel
                                                                                   version
                                                                               }
                                                                            && version.Dependencies.Any(y => y.Label
                                                                                == Model.Label
                                                                                && y.Namespace
                                                                                == Model.Namespace
                                                                                && y.ProjectId
                                                                                == Model.ProjectId))
                                                                   .Select(x => (x.Info!.Label, x.Info!.Namespace,
                                                                                           x.Info!.ProjectId,
                                                                                           (string?)
                                                                                           ((InstancePackageVersionModel)
                                                                                               x.Info!.Version).Id)),
                                                                    Filter.None);
            var tasks = dependants
                       .Select(async x =>
                        {
                            var count =
                                (uint)Collection.Count(y => y.Info!.Version is InstancePackageVersionModel version
                                                         && version.Dependencies.Any(z => z.Label == x.Label
                                                             && z.Namespace == x.Namespace
                                                             && z.ProjectId == x.ProjectId));
                            var found = Collection.FirstOrDefault(y => y.Info?.Label == x.Label
                                                                    && y.Info?.Namespace == x.Namespace
                                                                    && y.Info?.ProjectId == x.ProjectId);
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

        _old = Model.Owner.Entry.Purl;
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
        if (Model.Owner.Entry.Purl != _old)
        {
            PersistenceService.AppendAction(new(Guard.Key,
                                                PersistenceService.ActionKind.EditPackage,
                                                _old,
                                                Model.Owner.Entry.Purl));
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
        Model.Version = InstancePackageUnspecifiedVersionModel.Default;
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

        Model.Owner.Tags.Add(tag);
    }

    [RelayCommand]
    private void RemoveTag(string? tag)
    {
        if (tag == null)
        {
            return;
        }

        var index = Model.Owner.Tags.IndexOf(tag);
        if (index >= 0)
        {
            Model.Owner.Tags.RemoveAt(index);
        }
    }

    private bool CanViewPackage(InstancePackageDependencyModel? model) => model is { Installed: { Info: not null } };


    [RelayCommand(CanExecute = nameof(CanViewPackage))]
    private void ViewPackage(InstancePackageDependencyModel? model)
    {
        if (model is { Installed: { Info: not null } installed })
        {
            OverlayService.PopModal(new InstancePackageModal
            {
                DataContext = installed.Info,
                Guard = Guard,
                DataService = DataService,
                OverlayService = OverlayService,
                PersistenceService = PersistenceService,
                Collection = Collection,
                Filter = Filter
            });
        }
    }

    #endregion
}
