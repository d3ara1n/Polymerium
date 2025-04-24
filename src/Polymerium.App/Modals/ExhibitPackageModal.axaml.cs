using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions.Repositories;
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

    public static readonly DirectProperty<ExhibitPackageModal, ICommand?> InstallCommandProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, ICommand?>(nameof(InstallCommand),
                                                                        o => o.InstallCommand,
                                                                        (o, v) => o.InstallCommand = v);

    public static readonly DirectProperty<ExhibitPackageModal, bool> IsDetailPanelVisibleProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, bool>(nameof(IsDetailPanelVisible),
                                                                   o => o.IsDetailPanelVisible,
                                                                   (o, v) => o.IsDetailPanelVisible = v);

    private static bool isDetailPanelVisible;

    public bool IsDetailPanelVisible
    {
        get => isDetailPanelVisible;
        set => SetAndRaise(IsDetailPanelVisibleProperty, ref isDetailPanelVisible, value);
    }

    public static readonly DirectProperty<ExhibitPackageModal, ExhibitModel> ExhibitProperty =
        AvaloniaProperty.RegisterDirect<ExhibitPackageModal, ExhibitModel>(nameof(Exhibit),
                                                                           o => o.Exhibit,
                                                                           (o, v) => o.Exhibit = v);

    public required ExhibitModel Exhibit
    {
        get;
        set => SetAndRaise(ExhibitProperty, ref field, value);
    }


    public ExhibitPackageModal() => InitializeComponent();

    public ICommand? InstallCommand
    {
        get;
        set => SetAndRaise(InstallCommandProperty, ref field, value);
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

    public required ProfileGuard Guard { get; init; }
    public required DataService DataService { get; init; }
    public required Filter Filter { get; init; }

    private void DismissButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
    }

    protected override async void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        await Guard.DisposeAsync();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        IsFilterEnabled = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsFilterEnabledProperty)
            LazyVersions = ConstructVersions();
    }

    private LazyObject ConstructVersions()
    {
        var lazy = new LazyObject(async t =>
        {
            var versions = (await DataService.InspectVersionsAsync(Package.Label,
                                                                   Package.Namespace,
                                                                   Package.ProjectId,
                                                                   IsFilterEnabled ? Filter : Filter.Empty)).ToArray();
            var project = Package;
            var rv = new ExhibitVersionCollection(versions
                                                 .Select(x => new ExhibitVersionModel(project.Label,
                                                             project.Namespace,
                                                             project.ProjectName,
                                                             project.ProjectId,
                                                             x.VersionName,
                                                             x.VersionId,
                                                             string.Join(",",
                                                                         x.Requirements.AnyOfLoaders
                                                                          .Select(LoaderHelper.ToDisplayName)),
                                                             string.Join(",", x.Requirements.AnyOfVersions),
                                                             string.Empty,
                                                             x.PublishedAt,
                                                             x.DownloadCount,
                                                             x.ReleaseType,
                                                             PackageHelper.ToPurl(x.Label,
                                                                 x.Namespace,
                                                                 x.ProjectId,
                                                                 x.VersionId)))
                                                 .ToList());
            if (Exhibit.InstalledVersionId != null)
            {
                var installed = rv.FirstOrDefault(x => x.VersionId == Exhibit.InstalledVersionId);
                if (installed != null)
                    Dispatcher.UIThread.Post(() =>
                    {
                        // TODO: 这里赋值没用，ItemsSource 还没绑定上
                        SelectedVersion = installed;
                        SelectedVersionMode = 0;
                    });
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SelectedVersionMode = 1;
                });
            }

            return rv;
        });

        return lazy;
    }
}