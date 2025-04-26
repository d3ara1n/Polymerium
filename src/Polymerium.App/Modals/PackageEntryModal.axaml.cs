using System.Linq;
using Avalonia;
using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Modals;

public partial class PackageEntryModal : Modal
{
    public static readonly DirectProperty<PackageEntryModal, LazyObject?> LazyVersionsProperty =
        AvaloniaProperty.RegisterDirect<PackageEntryModal, LazyObject?>(nameof(LazyVersions),
                                                                        o => o.LazyVersions,
                                                                        (o, v) => o.LazyVersions = v);

    public static readonly DirectProperty<PackageEntryModal, bool> IsFilterEnabledProperty =
        AvaloniaProperty.RegisterDirect<PackageEntryModal, bool>(nameof(IsFilterEnabled),
                                                                 o => o.IsFilterEnabled,
                                                                 (o, v) => o.IsFilterEnabled = v);

    public static readonly DirectProperty<PackageEntryModal, InstancePackageVersionModel?>
        SelectedVersionProxyProperty =
            AvaloniaProperty
               .RegisterDirect<PackageEntryModal, InstancePackageVersionModel?>(nameof(SelectedVersionProxy),
                                                                                    o => o.SelectedVersionProxy,
                                                                                    (o, v) =>
                                                                                        o.SelectedVersionProxy = v);


    public PackageEntryModal() => InitializeComponent();

    public required ProfileGuard Guard { get; init; }
    public required DataService DataService { get; init; }
    public required Filter Filter { get; init; }

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


    private LazyObject ConstructVersions()
    {
        var lazy = new LazyObject(async t =>
                                  {
                                      if (t.IsCancellationRequested)
                                          return null;
                                      var versions = await DataService.InspectVersionsAsync(Model.Label,
                                                         Model.Namespace,
                                                         Model.ProjectId,
                                                         IsFilterEnabled ? Filter : Filter.Empty);
                                      return new InstancePackageVersionCollection(versions
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
                                                                                               x.ReleaseType))
                                         .ToList());
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

        IsFilterEnabled = true;
    }

    protected override async void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

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

    private void DismissButton_Click(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
        if (Model.Version is InstancePackageVersionModel v)
            v.IsCurrent = true;
    }

    private void RemoveVersionButton_Click(object? sender, RoutedEventArgs e)
    {
        Model.Version = InstancePackageUnspecifiedVersionModel.Instance;
        SelectedVersionProxy = null;
    }
}