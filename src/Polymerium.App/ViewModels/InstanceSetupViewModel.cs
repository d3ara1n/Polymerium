using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceSetupViewModel : InstanceViewModelBase
{
    internal const string DATAFORMATS = "never-gonna-give-you-up";

    private CancellationTokenSource? _cancellationTokenSource;
    private IDisposable? _updatingSubscription;

    public InstanceSetupViewModel(
        ViewBag bag,
        ProfileManager profileManager,
        RepositoryAgent repositories,
        IHttpClientFactory clientFactory,
        NotificationService notificationService,
        InstanceManager instanceManager) : base(bag, instanceManager, profileManager)
    {
        _repositories = repositories;
        _clientFactory = clientFactory;
        _notificationService = notificationService;
    }

    private void TriggerRefresh(CancellationToken token)
    {
        var inner = _cancellationTokenSource?.Token ?? token;
        try
        {
            if (ProfileManager.TryGetImmutable(Basic.Key, out var profile))
            {
                Stage.Clear();
                Stash.Clear();
                Draft.Clear();
                var stages = profile.Setup.Stage.Select(Load).ToList();
                var stashes = profile.Setup.Stash.Select(Load).ToList();
                var drafts = profile.Setup.Draft.Select(Load).ToList();
                StageCount = stages.Count;
                StashCount = stashes.Count;
                Stage.AddOrUpdate(stages);
                Stash.AddOrUpdate(stashes);
                Draft.AddRange(drafts);
            }
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex.Message, "Loading package list failed", NotificationLevel.Warning);
        }

        if (Basic.Source is not null && PackageHelper.TryParse(Basic.Source, out var r))
            Task.Run(() => LoadReference(r.Label, r.Namespace, r.Pid, r.Vid), inner);

        async Task LoadReference(string label, string? @namespace, string pid, string? vid)
        {
            try
            {
                var package = await _repositories.ResolveAsync(label,
                                                               @namespace,
                                                               pid,
                                                               vid,
                                                               Filter.Empty with { Kind = ResourceKind.Modpack });

                Bitmap thumbnail;
                if (!Debugger.IsAttached && package.Thumbnail is not null)
                {
                    using var client = _clientFactory.CreateClient();
                    var bytes = await client.GetByteArrayAsync(package.Thumbnail, token);
                    thumbnail = new Bitmap(new MemoryStream(bytes));
                }
                else
                {
                    thumbnail = AssetUriIndex.DIRT_IMAGE_BITMAP;
                }

                var page = await (await _repositories.InspectAsync(label,
                                                                   @namespace,
                                                                   pid,
                                                                   Filter.Empty with { Kind = ResourceKind.Modpack }))
                              .FetchAsync();
                var versions = page
                              .Select(x => new InstanceVersionModel(x.Label,
                                                                    x.Namespace,
                                                                    x.ProjectId,
                                                                    x.VersionId,
                                                                    x.VersionName,
                                                                    x.ReleaseType,
                                                                    x.PublishedAt)
                               {
                                   IsCurrent = x.VersionId == package.VersionId
                               })
                              .ToList();

                Dispatcher.UIThread.Post(() =>
                {
                    Reference = new InstanceReferenceModel
                    {
                        Name = package.ProjectName,
                        Thumbnail = thumbnail,
                        SourceUrl = package.Reference,
                        SourceLabel = package.Label,
                        Versions = versions,
                        CurrentVersion = versions.FirstOrDefault()
                    };
                });
            }
            catch (Exception ex)
            {
                _notificationService.PopMessage($"{Basic.Source}: {ex.Message}",
                                                "Fetching modpack information failed",
                                                NotificationLevel.Warning);
            }
        }

        InstancePackageModel Load(string purl)
        {
            InstancePackageModel model = new(purl);
            model.Task = Task.Run(async () =>
                                  {
                                      if (PackageHelper.TryParse(model.Purl, out var v))
                                          try
                                          {
                                              var p = await _repositories.ResolveAsync(v.Label,
                                                          v.Namespace,
                                                          v.Pid,
                                                          v.Vid,
                                                          Filter.Empty);

                                              if (!Debugger.IsAttached && p.Thumbnail is not null)
                                              {
                                                  var c = _clientFactory.CreateClient();
                                                  var b = await c.GetByteArrayAsync(p.Thumbnail,
                                                              _cancellationTokenSource?.Token
                                                           ?? CancellationToken.None);
                                                  model.Thumbnail = new Bitmap(new MemoryStream(b));
                                              }
                                              else
                                              {
                                                  model.Thumbnail = AssetUriIndex.DIRT_IMAGE_BITMAP;
                                              }

                                              model.Name = p.ProjectName;
                                              model.Version = p.VersionName;
                                              model.Summary = p.Summary;
                                              model.Kind = p.Kind;
                                              model.Reference = p.Reference;
                                              model.IsLoaded = true;
                                          }
                                          catch (Exception ex)
                                          {
                                              _notificationService.PopMessage($"{purl}: {ex.Message}",
                                                                              "Failed to parse purl",
                                                                              NotificationLevel.Warning);
                                          }
                                  },
                                  inner);
            return model;
        }
    }

    protected override void OnUpdateModel(string key, Profile profile)
    {
        if (profile.Setup.Loader is not null && LoaderHelper.TryParse(profile.Setup.Loader, out var result))
        {
            var loader = result.Identity switch
            {
                LoaderHelper.LOADERID_FORGE => "Forge",
                LoaderHelper.LOADERID_NEOFORGE => "NeoForge",
                LoaderHelper.LOADERID_FABRIC => "Fabric",
                LoaderHelper.LOADERID_QUILT => "Quilt",
                LoaderHelper.LOADERID_FLINT => "Flint",
                _ => result.Identity
            };
            LoaderLabel = $"{loader}/{result.Version}";
        }
        else
        {
            LoaderLabel = "None";
        }

        _updatingSubscription?.Dispose();
        UpdatingPending = true;
        UpdatingProgress = 0;

        base.OnUpdateModel(key, profile);
    }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (InstanceManager.IsTracking(Basic.Key, out var tracker) && tracker is UpdateTracker update)
            TrackUpdateProgress(update);
        else
            TriggerRefresh(_cancellationTokenSource?.Token ?? token);

        await base.OnInitializedAsync(token);
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        _cancellationTokenSource?.Cancel();
        _updatingSubscription?.Dispose();

        return base.OnDeinitializeAsync(token);
    }

    protected override void OnInstanceUpdating(UpdateTracker tracker)
    {
        TrackUpdateProgress(tracker);
        base.OnInstanceUpdating(tracker);
    }

    protected override void OnInstanceUpdated(UpdateTracker tracker)
    {
        TriggerRefresh(_cancellationTokenSource?.Token ?? CancellationToken.None);
        base.OnInstanceUpdated(tracker);
    }

    private void TrackUpdateProgress(UpdateTracker update)
    {
        _updatingSubscription?.Dispose();
        _updatingSubscription = Observable
                               .Interval(TimeSpan.FromSeconds(1))
                               .Subscribe(_ =>
                                {
                                    var progress = update.Progress;
                                    Dispatcher.UIThread.Post(() =>
                                    {
                                        if (progress.HasValue)
                                        {
                                            UpdatingProgress = progress.Value;
                                            UpdatingPending = false;
                                        }
                                        else
                                        {
                                            UpdatingPending = true;
                                        }
                                    });
                                });
    }

    #region Commands

    [RelayCommand]
    private void OpenSourceUrl(Uri? url)
    {
        if (url is not null)
            Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }

    private bool CanUpdate(InstanceVersionModel? model) => model is { IsCurrent: false };

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private void Update(InstanceVersionModel? model)
    {
        if (model is null)
            return;

        try
        {
            InstanceManager.Update(Basic.Key, model.Label, model.Namespace, model.Pid, model.Vid);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex.Message, "Update failed", NotificationLevel.Danger);
        }
    }

    #endregion

    #region Injected

    private readonly RepositoryAgent _repositories;
    private readonly IHttpClientFactory _clientFactory;
    private readonly NotificationService _notificationService;

    #endregion

    #region Reactive

    [ObservableProperty]
    private InstanceReferenceModel? _reference;

    [ObservableProperty]
    private string _loaderLabel;

    [ObservableProperty]
    private SourceCache<InstancePackageModel, string> _stage = new(x => x.Purl);

    [ObservableProperty]
    private SourceCache<InstancePackageModel, string> _stash = new(x => x.Purl);

    [ObservableProperty]
    private AvaloniaList<InstancePackageModel> _draft = [];

    [ObservableProperty]
    private int _stageCount;

    [ObservableProperty]
    private int _stashCount;

    [ObservableProperty]
    private double _updatingProgress;

    [ObservableProperty]
    private bool _updatingPending = true;

    #endregion
}