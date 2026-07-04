using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.States;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using Polymerium.Avalonia.Widgets;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.PageModels;

public partial class InstancePageModel : ViewModelBase,
    IStatefulViewModel<InstancePageModel.SidebarState>
{
    public InstancePageModel(
        IViewContext context,
        OverlayService overlayService,
        ProfileManager profileManager,
        InstanceStateAggregator aggregator,
        WidgetHostService widgetHostService,
        NotificationService notificationService,
        DataService dataService,
        PersistenceService persistenceService,
        InstanceService instanceService
    )
    {
        _profileManager = profileManager;
        _overlayService = overlayService;
        _notificationService = notificationService;
        _dataService = dataService;
        _persistenceService = persistenceService;
        _aggregator = aggregator;
        _instanceService = instanceService;
        SelectedPage =
            context.Parameter switch
            {
                CompositeParameter it => PageEntries.FirstOrDefault(x => x.Page == it.Subview),
                _ => null,
            } ?? PageEntries.FirstOrDefault();

        var key = context.Parameter switch
        {
            CompositeParameter p => p.Key,
            string s => s,
            _ => throw new PageNotReachedException(
                typeof(InstancePage),
                "Key to the instance is not provided"
            ),
        };
        if (profileManager.TryGetImmutable(key, out var profile))
        {
            Basic = new(
                key,
                profile.Name,
                profile.Setup.Version,
                profile.Setup.Loader,
                profile.Setup.Source
            );
            Context = new(
                Basic,
                [
                    .. widgetHostService.WidgetTypes.Select(type =>
                    {
                        var widget = (WidgetBase)Activator.CreateInstance(type)!;
                        widget.Context = widgetHostService.GetOrCreateContext(Basic.Key, type.Name);
                        return widget;
                    }),
                ]
            );
        }
        else
        {
            throw new PageNotReachedException(
                typeof(InstancePage),
                Resources.InstancePage_KeyNotFoundExceptionMessage.Replace("{0}", key)
            );
        }
    }

    #region Nested type: CompositeParameter

    public record CompositeParameter(string Key, Type Subview);

    #endregion

    #region Commands

    [RelayCommand]
    private Task OpenFolder() => _instanceService.OpenFolder(Basic.Key);

    [RelayCommand]
    private Task ExportInstance() => _instanceService.ExportInstanceAsync(Basic.Key);

    [RelayCommand]
    private void ManageSnapshotsAsync()
    {
        _overlayService.PopModal<SnapshotsModal>(Basic);
    }

    [RelayCommand]
    public async Task ImportFromFileAsync(string? initialPath)
    {
        // 这里应该是 AssetImportDialog 才对
        // AssetIdentificationModel { AssetIdentificationPackageModel, AssetIdentificationPersistModel { IsInImportMode: bool } }
        // Result is AssetIdentificationPackageModel package
        //  or AssetIdentificationPersistModel { IsInImportMode: false } persist
        //  or AssetIdentificationPersistModel { IsInImportMode: true } import
        var dialog = new AssetImporterDialog
        {
            PathAccepted = initialPath,
            DataService = _dataService,
            NotificationService = _notificationService,
        };
        if (await _overlayService.PopDialogAsync(dialog))
        {
            switch (dialog.Result)
            {
                case AssetIdentificationPackageModel package:
                    if (_profileManager.TryGetMutable(Basic.Key, out var guard))
                    {
                        await using (guard)
                        {
                            if (
                                !guard.Value.Setup.Packages.Any(x =>
                                    PackageHelper.IsMatched(
                                        x.Purl,
                                        package.Package.Label,
                                        package.Package.Namespace,
                                        package.Package.ProjectId
                                    )
                                )
                            )
                            {
                                var purl = PackageHelper.ToPurl(
                                    package.Package.Label,
                                    package.Package.Namespace,
                                    package.Package.ProjectId,
                                    package.Package.VersionId
                                );
                                guard.Value.Setup.Packages.Add(
                                    new()
                                    {
                                        Purl = purl,
                                        Enabled = true,
                                        Source = null,
                                    }
                                );
                                _persistenceService.AppendAction(
                                    new()
                                    {
                                        Key = Basic.Key,
                                        Kind = PersistenceService.ActionKind.EditPackage,
                                        New = purl,
                                    }
                                );
                                _notificationService.PopMessage(
                                    Resources
                                        .InstancePage_ImportPackageSuccessNotificationMessage.Replace(
                                            "{0}",
                                            package.Package.ProjectName
                                        )
                                        .Replace("{1}", package.Package.ProjectId),
                                    guard.Key,
                                    thumbnail: package.Thumbnail
                                );
                            }
                            else
                            {
                                _notificationService.PopMessage(
                                    Resources
                                        .InstancePage_ImportPackageAlreadyExistsDangerNotificationMessage.Replace(
                                            "{0}",
                                            package.Package.ProjectName
                                        )
                                        .Replace("{1}", package.Package.ProjectId),
                                    Resources.InstancePage_ImportPackageAlreadyExistsDangerNotificationTitle,
                                    GrowlLevel.Danger,
                                    thumbnail: package.Thumbnail
                                );
                            }
                        }
                    }

                    break;
                case AssetIdentificationPersistModel persist:
                    var target = Path.Combine(
                        persist.IsInImportMode
                            ? PathDef.Default.DirectoryOfImport(Basic.Key)
                            : PathDef.Default.DirectoryOfPersist(Basic.Key),
                        FileHelper.GetAssetFolderName(persist.Kind),
                        Path.GetFileName(persist.Path)
                    );
                    if (!File.Exists(target))
                    {
                        var dir = Path.GetDirectoryName(target);
                        if (dir != null && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        File.Copy(persist.Path, target, false);
                        _notificationService.PopMessage(
                            Resources.InstancePage_ImportFileSuccessNotificationMessage.Replace(
                                "{0}",
                                target
                            ),
                            Basic.Key,
                            thumbnail: ThumbnailHelper.ForInstance(Basic.Key)
                        );
                    }
                    else
                    {
                        var relative = Path.GetRelativePath(
                            PathDef.Default.DirectoryOfHome(Basic.Key),
                            target
                        );
                        _notificationService.PopMessage(
                            Resources.InstancePage_ImportFileAlreadyExistsDangerNotificationMessage.Replace(
                                "{0}",
                                relative
                            ),
                            Resources.InstancePage_ImportFileAlreadyExistsDangerNotificationTitle,
                            GrowlLevel.Danger,
                            thumbnail: ThumbnailHelper.ForInstance(Basic.Key)
                        );
                    }

                    break;
            }
        }
    }

    #endregion

    #region Direct

    public InstanceBasicModel Basic { get; }
    public InstancePageModelBase.InstanceContextParameter Context { get; }

    #endregion


    #region Injected

    private readonly ProfileManager _profileManager;
    private readonly OverlayService _overlayService;
    private readonly DataService _dataService;
    private readonly NotificationService _notificationService;
    private readonly PersistenceService _persistenceService;
    private readonly InstanceStateAggregator _aggregator;
    private readonly InstanceService _instanceService;

    #endregion

    #region Tracking

    private IDisposable? _aggregatorSubscription;

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        _aggregatorSubscription = _aggregator.Watch(Basic.Key).Subscribe(snapshot =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                State = snapshot?.State ?? InstanceState.Idle;
            });
        });

        _profileManager.ProfileUpdated += OnProfileUpdated;

        foreach (var widget in Context.Widgets)
        {
            await widget.InitializeAsync();
        }
    }

    protected override async Task OnDeinitializeAsync()
    {
        _aggregatorSubscription?.Dispose();
        _profileManager.ProfileUpdated -= OnProfileUpdated;

        foreach (var widget in Context.Widgets)
        {
            await widget.DeinitializeAsync();
        }
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        if (e.Key != Basic.Key)
        {
            return;
        }

        Basic.Name = e.Value.Name;
        Basic.Version = e.Value.Setup.Version;
        Basic.Loader = e.Value.Setup.Loader;
        Basic.Source = e.Value.Setup.Source;
        Basic.UpdateIcon();
    }

    #endregion

    #region Reactive

    public ObservableCollection<InstanceSubpageEntryModel> PageEntries { get; } =
    [
        // Home
        new(typeof(InstanceHomePage), Symbol.Home, Resources.InstancePage_HomePageText),
        // Dashboard
        new(
            typeof(InstanceDashboardPage),
            Symbol.PulseSquare,
            Resources.InstancePage_DashboardPageText
        ),
        // Setup
        new(typeof(InstanceSetupPage), Symbol.Apps, Resources.InstancePage_SetupPageText),
        // Files
        new(typeof(InstanceFilesPage), Symbol.DocumentFolder, Resources.InstancePage_FilesPageText),
        // Workspace
        new(
            typeof(InstanceWorkspacePage),
            Symbol.ArrowSyncCircle,
            Resources.InstancePage_WorkspacePageText
        ),
        // Widgets
        new(typeof(InstanceWidgetsPage), Symbol.AppFolder, Resources.InstancePage_WidgetsPageText),
        // Statistics
        new(
            typeof(InstanceActivitiesPage),
            Symbol.DataArea,
            Resources.InstancePage_StatisticsPageText
        ),
        // Storage
        new(
            typeof(InstanceStoragePage),
            Symbol.ChartMultiple,
            Resources.InstancePage_StoragePageText
        ),
        // Properties
        new(
            typeof(InstancePropertiesPage),
            Symbol.Wrench,
            Resources.InstancePage_PropertiesPageText
        ),
    ];

    [ObservableProperty]
    public partial InstanceSubpageEntryModel? SelectedPage { get; set; }

    [ObservableProperty]
    public partial InstanceState State { get; set; } = InstanceState.Idle;

    [ObservableProperty]
    public partial SidebarState? ViewState { get; set; }

    [RelayCommand]
    private void ToggleSidebar() => ViewState?.IsSidebarExpanded = !ViewState.IsSidebarExpanded;

    #endregion

    #region Nested type: SidebarState

    public partial class SidebarState : ModelBase
    {
        [ObservableProperty]
        public partial bool IsSidebarExpanded { get; set; } = true;
    }

    #endregion
}
