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
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using Polymerium.Avalonia.Widgets;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.PageModels;

public partial class InstancePageModel : ViewModelBase, IStatefulViewModel<InstancePageModel.SidebarState>
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
        InstanceService instanceService)
    {
        _profileManager = profileManager;
        _overlayService = overlayService;
        _notificationService = notificationService;
        _dataService = dataService;
        _persistenceService = persistenceService;
        _aggregator = aggregator;
        _instanceService = instanceService;
        SelectedPage = context.Parameter switch
        {
            CompositeParameter it => PageEntries.FirstOrDefault(x => x.Page == it.Subview),
            _ => null
        }
                    ?? PageEntries.FirstOrDefault();

        var key = context.Parameter switch
        {
            CompositeParameter p => p.Key,
            string s => s,
            _ => throw new PageNotReachedException(typeof(InstancePage), "Key to the instance is not provided")
        };
        if (profileManager.TryGetImmutable(key, out var profile))
        {
            Basic = new(key, profile.Name, profile.Setup.Version, profile.Setup.Loader, profile.Setup.Source);
            Context = new(Basic,
            [
                .. widgetHostService.WidgetTypes.Select(type =>
                {
                    var widget = (WidgetBase)Activator.CreateInstance(type)!;
                    widget.Context = widgetHostService.GetOrCreateContext(Basic.Key, type.Name);
                    return widget;
                })
            ]);
        }
        else
        {
            throw new PageNotReachedException(typeof(InstancePage),
                                              LanguageManager.Instance.InstancePage_KeyNotFoundExceptionMessage.Current().Replace("{0}", key));
        }
    }

    #region Nested type: CompositeParameter

    public record CompositeParameter(string Key, Type Subview);

    #endregion

    #region Nested type: SidebarState

    public partial class SidebarState : ModelBase
    {
        [ObservableProperty]
        public partial bool IsSidebarExpanded { get; set; } = true;
    }

    #endregion

    #region Commands

    [RelayCommand]
    private Task OpenFolder() => _instanceService.OpenFolder(Basic.Key);

    [RelayCommand]
    private Task ExportInstance() => _instanceService.ExportInstanceAsync(Basic.Key);

    [RelayCommand]
    private void ManageSnapshotsAsync() => _overlayService.PopModal<SnapshotsModal>(Basic);

    [RelayCommand]
    public async Task ImportFromFileAsync(string? initialPath)
    {
        // TODO: 这里应为 AssetImportDialog（返回 AssetIdentificationPackageModel / AssetIdentificationPersistModel），
        //  目前暂用 AssetImporterDialog。
        var dialog = new AssetImporterDialog
        {
            PathAccepted = initialPath,
            DataService = _dataService,
            NotificationService = _notificationService
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
                            if (!guard.Value.Setup.Packages.Any(x => PackageHelper.IsMatched(x.Pref,
                                                                    package.Package.Label,
                                                                    package.Package.Namespace,
                                                                    package.Package.ProjectId)))
                            {
                                var pref = PackageHelper.ToPref(package.Package.Label,
                                                                package.Package.Namespace,
                                                                package.Package.ProjectId,
                                                                package.Package.VersionId);
                                guard.Value.Setup.Packages.Add(new() { Pref = pref, Enabled = true, Source = null });
                                _persistenceService.AppendAction(new()
                                {
                                    Key = Basic.Key,
                                    Kind = PersistenceService.ActionKind
                                                             .EditPackage,
                                    New = pref
                                });
                                _notificationService.PopMessage(LanguageManager.Instance.InstancePage_ImportPackageSuccessNotificationMessage.Current()
                                                               .Replace("{0}", package.Package.ProjectName)
                                                               .Replace("{1}", package.Package.ProjectId),
                                                                guard.Key,
                                                                thumbnail: package.Thumbnail);
                            }
                            else
                            {
                                _notificationService.PopMessage(LanguageManager.Instance.InstancePage_ImportPackageAlreadyExistsDangerNotificationMessage.Current()
                                                               .Replace("{0}", package.Package.ProjectName)
                                                               .Replace("{1}", package.Package.ProjectId),
                                                                LanguageManager.Instance.InstancePage_ImportPackageAlreadyExistsDangerNotificationTitle.Current(),
                                                                GrowlLevel.Danger,
                                                                thumbnail: package.Thumbnail);
                            }
                        }
                    }

                    break;
                case AssetIdentificationPersistModel persist:
                    var target =
                        Path.Combine(persist.IsInImportMode
                                         ? PathDef.Default.DirectoryOfImport(Basic.Key)
                                         : PathDef.Default.DirectoryOfPersist(Basic.Key),
                                     FileHelper.GetAssetFolderName(persist.Kind),
                                     Path.GetFileName(persist.Path));
                    if (!File.Exists(target))
                    {
                        var dir = Path.GetDirectoryName(target);
                        if (dir != null && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        File.Copy(persist.Path, target, false);
                        _notificationService.PopMessage(LanguageManager.Instance.InstancePage_ImportFileSuccessNotificationMessage.Current()
                                                                 .Replace("{0}", target),
                                                        Basic.Key,
                                                        thumbnail: ThumbnailHelper.ForInstance(Basic.Key));
                    }
                    else
                    {
                        var relative = Path.GetRelativePath(PathDef.Default.DirectoryOfHome(Basic.Key), target);
                        _notificationService.PopMessage(LanguageManager.Instance.InstancePage_ImportFileAlreadyExistsDangerNotificationMessage.Current()
                                                       .Replace("{0}", relative),
                                                        LanguageManager.Instance.InstancePage_ImportFileAlreadyExistsDangerNotificationTitle.Current(),
                                                        GrowlLevel.Danger,
                                                        thumbnail: ThumbnailHelper.ForInstance(Basic.Key));
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
        _aggregatorSubscription = _aggregator
                                 .Watch(Basic.Key)
                                 .Subscribe(snapshot =>
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
        new(typeof(InstanceHomePage), Symbol.Home, "InstancePage_HomePageText"),
        new(typeof(InstanceDashboardPage), Symbol.PulseSquare, "InstancePage_DashboardPageText"),
        new(typeof(InstanceSetupPage), Symbol.Apps, "InstancePage_SetupPageText"),
        new(typeof(InstanceFilesPage), Symbol.DocumentFolder, "InstancePage_FilesPageText"),
        new(typeof(InstanceWorkspacePage), Symbol.ArrowSyncCircle, "InstancePage_WorkspacePageText"),
        new(typeof(InstanceWidgetsPage), Symbol.AppFolder, "InstancePage_WidgetsPageText"),
        new(typeof(InstanceActivitiesPage), Symbol.DataArea, "InstancePage_StatisticsPageText"),
        new(typeof(InstanceStoragePage), Symbol.ChartMultiple, "InstancePage_StoragePageText"),
        new(typeof(InstancePropertiesPage), Symbol.Wrench, "InstancePage_PropertiesPageText")
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
}
