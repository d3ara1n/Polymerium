using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Modals;

public partial class ExhibitPackageModal : Modal
{
    public static readonly DirectProperty<ExhibitPackageModal, LazyObject?> LazyVersionsProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, LazyObject?>(nameof(LazyVersions),
                                                                          o => o.LazyVersions,
                                                                          (o, v) => o.LazyVersions = v);

    public static readonly DirectProperty<ExhibitPackageModal, bool> IsFilterEnabledProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, bool>(nameof(IsFilterEnabled),
                                                                   o => o.IsFilterEnabled,
                                                                   (o, v) => o.IsFilterEnabled = v);

    public static readonly DirectProperty<ExhibitPackageModal, ExhibitVersionModel?> SelectedVersionProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, ExhibitVersionModel?>(nameof(SelectedVersion),
            o => o.SelectedVersion,
            (o, v) => o.SelectedVersion = v);

    public static readonly DirectProperty<ExhibitPackageModal, int> SelectedVersionModeProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, int>(nameof(SelectedVersionMode),
                                                                  o => o.SelectedVersionMode,
                                                                  (o, v) => o.SelectedVersionMode = v);

    public static readonly DirectProperty<ExhibitPackageModal, bool> IsDetailPanelVisibleProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, bool>(nameof(IsDetailPanelVisible),
                                                                   o => o.IsDetailPanelVisible,
                                                                   (o, v) => o.IsDetailPanelVisible = v);

    public static readonly DirectProperty<ExhibitPackageModal, ExhibitModel> ExhibitProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, ExhibitModel>(nameof(Exhibit),
                                                                           o => o.Exhibit,
                                                                           (o, v) => o.Exhibit = v);

    public static readonly DirectProperty<ExhibitPackageModal, LazyObject?> LazyDescriptionProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, LazyObject?>(nameof(LazyDescription),
                                                                          o => o.LazyDescription,
                                                                          (o, v) => o.LazyDescription = v);

    public static readonly DirectProperty<ExhibitPackageModal, LazyObject?> LazyDependenciesProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, LazyObject?>(nameof(LazyDependencies),
                                                                          o => o.LazyDependencies,
                                                                          (o, v) => o.LazyDependencies = v);

    public static readonly DirectProperty<ExhibitPackageModal, ICommand?> ViewPackageCommandProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, ICommand?>(nameof(ViewPackageCommand),
                                                                        o => o.ViewPackageCommand,
                                                                        (o, v) => o.ViewPackageCommand = v);

    public static readonly DirectProperty<ExhibitPackageModal, LazyObject?> LazyChangelogProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, LazyObject?>(nameof(LazyChangelog),
                                                                          o => o.LazyChangelog,
                                                                          (o, v) => o.LazyChangelog = v);

    public LazyObject? LazyChangelog
    {
        get;
        set => SetAndRaise(LazyChangelogProperty, ref field, value);
    }


    public ICommand? ViewPackageCommand
    {
        get;
        set => SetAndRaise(ViewPackageCommandProperty, ref field, value);
    }


    public LazyObject? LazyDependencies
    {
        get;
        set => SetAndRaise(LazyDependenciesProperty, ref field, value);
    }


    public LazyObject? LazyDescription
    {
        get;
        set => SetAndRaise(LazyDescriptionProperty, ref field, value);
    }


    public ExhibitPackageModal() => InitializeComponent();


    private static bool isDetailPanelVisible;

    public bool IsDetailPanelVisible
    {
        get;
        set => SetAndRaise(IsDetailPanelVisibleProperty, ref field, value);
    } = isDetailPanelVisible;

    public required ExhibitModel Exhibit
    {
        get;
        set => SetAndRaise(ExhibitProperty, ref field, value);
    }


    public int SelectedVersionMode
    {
        get;
        set => SetAndRaise(SelectedVersionModeProperty, ref field, value);
    } = 1;


    private ExhibitPackageModel Package => (DataContext as ExhibitPackageModel)!;

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

    public ExhibitVersionModel? SelectedVersion
    {
        get;
        set => SetAndRaise(SelectedVersionProperty, ref field, value);
    }

    public required DataService DataService { get; init; }
    public required Filter Filter { get; init; }

    public required Action<ExhibitModel> ModifyPendingCallback { get; init; }
    public required Func<Project, ExhibitModel> LinkExhibitCallback { get; init; }


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Trigger ConstructVersions
        IsFilterEnabled = true;

        // Initialize
        LazyDescription = ConstructDescription();
        LazyChangelog = ConstructChangelog();
        LazyDependencies = ConstructDependencies();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsFilterEnabledProperty)
            LazyVersions = ConstructVersions();
        if (change.Property == SelectedVersionProperty || change.Property == SelectedVersionModeProperty)
            LazyDependencies = ConstructDependencies();
        if (change.Property == SelectedVersionProperty)
            LazyChangelog = ConstructChangelog();
        if (change.Property == IsDetailPanelVisibleProperty)
            isDetailPanelVisible = change.GetNewValue<bool>();
    }

    private LazyObject ConstructDescription()
    {
        var lazy = new LazyObject(async t =>
        {
            if (t.IsCancellationRequested)
                return null;
            var description = await DataService.ReadDescriptionAsync(Package.Label,
                                                                     Package.Namespace,
                                                                     Package.ProjectId);
            return description;
        });
        return lazy;
    }

    private LazyObject ConstructChangelog()
    {
        var lazy = new LazyObject(async t =>
        {
            if (t.IsCancellationRequested)
                return null;
            var vid = SelectedVersionMode == 0 ? SelectedVersion?.VersionId : null;
            if (vid is null)
                return null;

            var description =
                await DataService.ReadChangelogAsync(Package.Label, Package.Namespace, Package.ProjectId, vid);
            return description;
        });
        return lazy;
    }

    private LazyObject ConstructDependencies()
    {
        var lazy = new LazyObject(async t =>
        {
            if (t.IsCancellationRequested)
                return null;
            var vid = SelectedVersionMode == 0 ? SelectedVersion?.VersionId : null;
            var package = await DataService.ResolvePackageAsync(Package.Label,
                                                                Package.Namespace,
                                                                Package.ProjectId,
                                                                vid,
                                                                Filter);
            var tasks = package
                       .Dependencies.Select(async x =>
                        {
                            var dependency = await DataService.QueryProjectAsync(x.Label, x.Namespace, x.Pid);
                            return new ExhibitDependencyModel(LinkExhibitCallback(dependency),
                                                              x.Label,
                                                              x.Namespace,
                                                              x.Pid,
                                                              x.Vid,
                                                              dependency.ProjectName,
                                                              dependency.Thumbnail ?? AssetUriIndex.DIRT_IMAGE,
                                                              dependency.Author,
                                                              dependency.Kind,
                                                              x.IsRequired);
                        })
                       .ToArray();
            await Task.WhenAll(tasks);
            return new ExhibitDependencyCollection(package.VersionName,
                                                   package.VersionId,
                                                   tasks.Select(x => x.Result).ToList());
        });
        return lazy;
    }

    private LazyObject ConstructVersions()
    {
        var lazy = new LazyObject(async t =>
                                  {
                                      if (t.IsCancellationRequested)
                                          return null;
                                      var versions = (await DataService.InspectVersionsAsync(Package.Label,
                                                          Package.Namespace,
                                                          Package.ProjectId,
                                                          IsFilterEnabled ? Filter : Filter.Empty)).ToArray();
                                      var project = Package;
                                      var rv = new ExhibitVersionCollection(versions
                                                                           .Select(x =>
                                                                                new ExhibitVersionModel(project.Label,
                                                                                    project.Namespace,
                                                                                    project.ProjectName,
                                                                                    project.ProjectId,
                                                                                    x.VersionName,
                                                                                    x.VersionId,
                                                                                    string.Join(",",
                                                                                        x.Requirements.AnyOfLoaders
                                                                                           .Select(LoaderHelper
                                                                                               .ToDisplayName)),
                                                                                    string.Join(",",
                                                                                        x.Requirements.AnyOfVersions),
                                                                                    string.Empty,
                                                                                    x.PublishedAt,
                                                                                    x.DownloadCount,
                                                                                    x.ReleaseType,
                                                                                    PackageHelper.ToPurl(x.Label,
                                                                                        x.Namespace,
                                                                                        x.ProjectId,
                                                                                        x.VersionId)))
                                                                           .ToList());
                                      return rv;
                                  },
                                  value =>
                                  {
                                      var versionId = Exhibit.State switch
                                      {
                                          null or ExhibitState.Editable or ExhibitState.Removing => Exhibit
                                             .InstalledVersionId,
                                          _ => Exhibit.PendingVersionId
                                      };
                                      if (versionId != null && value is ExhibitVersionCollection versions)
                                      {
                                          var installed = versions.FirstOrDefault(x => x.VersionId == versionId);
                                          if (installed != null)
                                          {
                                              SelectedVersion = installed;
                                              SelectedVersionMode = 0;
                                          }
                                      }
                                      else
                                      {
                                          Dispatcher.UIThread.Post(() =>
                                          {
                                              SelectedVersionMode = 1;
                                          });
                                      }
                                  });

        return lazy;
    }

    #region Commands

    [RelayCommand]
    private void Dismiss()
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
    }

    [RelayCommand]
    private void Apply()
    {
        if (Exhibit.State is null)
            Exhibit.State = ExhibitState.Adding;

        if (Exhibit.State is ExhibitState.Editable)
        {
            if (Exhibit.InstalledVersionId == SelectedVersion?.VersionId)
            {
                // 未作出更改，Abort
                Dismiss();
                return;
            }

            Exhibit.State = ExhibitState.Modifying;
        }

        if (SelectedVersionMode == 0 && SelectedVersion != null)
        {
            // 指定了版本
            Exhibit.PendingVersionId = SelectedVersion?.VersionId;
            Exhibit.PendingVersionName = SelectedVersion?.VersionName;
        }
        else
        {
            // 未指定版本
            Exhibit.PendingVersionId = null;
            Exhibit.PendingVersionName = null;
        }

        ModifyPendingCallback(Exhibit);
        Dismiss();
    }

    [RelayCommand]
    private void Delete()
    {
        Exhibit.State = ExhibitState.Removing;
        Exhibit.PendingVersionId = null;
        Exhibit.PendingVersionName = null;
        ModifyPendingCallback(Exhibit);
        Dismiss();
    }

    [RelayCommand]
    private void Undo()
    {
        Exhibit.State = Exhibit.Installed == null ? null : ExhibitState.Editable;
        Exhibit.PendingVersionId = null;
        Exhibit.PendingVersionName = null;
        ModifyPendingCallback(Exhibit);
        Dismiss();
    }

    #endregion
}