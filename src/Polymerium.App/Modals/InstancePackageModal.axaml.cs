using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

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

    private string? _old;


    public InstancePackageModal() => InitializeComponent();

    public required ProfileGuard Guard { get; init; }
    public required DataService DataService { get; init; }
    public required Filter Filter { get; init; }
    public required OverlayService OverlayService { get; init; }
    public required PersistenceService PersistenceService { get; init; }
    public required IReadOnlyList<InstancePackageModel> OriginalCollection { get; init; }

    public LazyObject? LazyDependencies
    {
        get;
        set => SetAndRaise(LazyDependenciesProperty, ref field, value);
    }


    private InstancePackageModel Model => (DataContext as InstancePackageModel)!;

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
                return null;
            var vid = SelectedVersionProxy?.Id;
            var package = await DataService.ResolvePackageAsync(Model.Label,
                                                                Model.Namespace,
                                                                Model.ProjectId,
                                                                vid,
                                                                Filter);
            var refCount = (uint)OriginalCollection.Count(x => x.Version is InstancePackageVersionModel version
                                                            && version.Dependencies.Any(z => z.Label == Model.Label
                                                                && z.Namespace == Model.Namespace
                                                                && z.Pid == Model.ProjectId));
            var strongRefCount = (uint)OriginalCollection.Count(x => x.Version is InstancePackageVersionModel version
                                                                  && version.Dependencies.Any(z => z.IsRequired
                                                                      && z.Label == Model.Label
                                                                      && z.Namespace == Model.Namespace
                                                                      && z.Pid == Model.ProjectId));
            var tasks = package
                       .Dependencies.Select(async x =>
                        {
                            var count =
                                (uint)OriginalCollection.Count(y => y.Version is InstancePackageVersionModel version
                                                                 && version.Dependencies.Any(z => z.Label == x.Label
                                                                     && z.Namespace == x.Namespace
                                                                     && z.Pid == x.Pid));
                            var found = OriginalCollection.FirstOrDefault(y => y.Label == x.Label
                                                                            && y.Namespace == x.Namespace
                                                                            && y.ProjectId == x.Pid);
                            if (found is not null)
                                return new InstancePackageDependencyModel(x.Label,
                                                                          x.Namespace,
                                                                          x.Pid,
                                                                          x.Vid,
                                                                          found.ProjectName,
                                                                          found.Thumbnail,
                                                                          count,
                                                                          x.IsRequired) { Installed = found };

                            var project = await DataService.QueryProjectAsync(x.Label, x.Namespace, x.Pid);
                            var thumbnail = project.Thumbnail is not null
                                                ? await DataService.GetBitmapAsync(project.Thumbnail)
                                                : AssetUriIndex.DIRT_IMAGE_BITMAP;
                            return new InstancePackageDependencyModel(x.Label,
                                                                      x.Namespace,
                                                                      x.Pid,
                                                                      x.Vid,
                                                                      project.ProjectName,
                                                                      thumbnail,
                                                                      count,
                                                                      x.IsRequired);
                        })
                       .ToArray();
            await Task.WhenAll(tasks);
            var rv = new InstancePackageDependencyCollection(refCount,
                                                             strongRefCount,
                                                             [.. tasks.Select(x => x.Result)]);
            return rv;
        });
        return lazy;
    }

    private LazyObject ConstructVersions()
    {
        var lazy = new LazyObject(async t =>
                                  {
                                      if (t.IsCancellationRequested)
                                          return null;
                                      var versions = await DataService.InspectVersionsAsync(Model.Label,
                                                         Model.Namespace,
                                                         Model.ProjectId,
                                                         IsFilterEnabled ? Filter : Filter.None);
                                      return new InstancePackageVersionCollection([.. versions
                                         .Select<Version,
                                              InstancePackageVersionModelBase>(x => Model is
                                                                                       {
                                                                                           Version:
                                                                                           InstancePackageVersionModel v
                                                                                       }
                                                                                    && v.Id == x.VersionId
                                                                                       ? v
                                                                                       : new
                                                                                           InstancePackageVersionModel(x.VersionId,
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
                                                                                               x.Dependencies))]);
                                  },
                                  _ =>
                                  {
                                      if (Model.Version is InstancePackageVersionModel v)
                                          SelectedVersionProxy = v;
                                  });

        return lazy;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _old = Model.Entry.Purl;
        IsFilterEnabled = true;
        LazyDependencies = ConstructDependencies();
    }

    protected override async void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        if (Model.Entry.Purl != _old)
            PersistenceService.AppendAction(new PersistenceService.Action(Guard.Key,
                                                                          PersistenceService.ActionKind.EditPackage,
                                                                          _old,
                                                                          Model.Entry.Purl));
        await Guard.DisposeAsync();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsFilterEnabledProperty)
            LazyVersions = ConstructVersions();
        if (change.Property == SelectedVersionProxyProperty
         && change.NewValue is InstancePackageVersionModel v
         && Model.Version != v)
            Model.Version = v;
    }


    #region Commands

    [RelayCommand]
    private void Dismiss()
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
        if (Model.Version is InstancePackageVersionModel v)
            v.IsCurrent = true;
    }

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
            return;

        Model.Tags.Add(tag);
    }

    [RelayCommand]
    private void RemoveTag(string? tag)
    {
        if (tag == null)
            return;

        var index = Model.Tags.IndexOf(tag);
        if (index >= 0)
            Model.Tags.RemoveAt(index);
    }

    private void ViewDependency(InstancePackageDependencyModel? model) { }

    #endregion
}