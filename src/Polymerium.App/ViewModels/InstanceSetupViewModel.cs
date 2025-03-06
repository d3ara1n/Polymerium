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

    private CancellationTokenSource? _pageCancellationTokenSource;
    private CancellationTokenSource? _updatingCancellationTokenSource;
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
        _updatingCancellationTokenSource?.Cancel();
        _updatingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        var inner = _updatingCancellationTokenSource.Token;
        var semaphore = new SemaphoreSlim(Math.Max(Environment.ProcessorCount / 2, 1));
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
        return;


        InstancePackageModel Load(string purl)
        {
            InstancePackageModel model = new(purl);
            model.Task = Task.Run(async () =>
                                  {
                                      if (PackageHelper.TryParse(model.Purl, out var v))
                                          try
                                          {
                                              if (inner.IsCancellationRequested)
                                                  return;
                                              await semaphore.WaitAsync(inner);
                                              var p = await _repositories.ResolveAsync(v.Label,
                                                          v.Namespace,
                                                          v.Pid,
                                                          v.Vid,
                                                          Filter.Empty);

                                              if (p.Thumbnail is not null)
                                              {
                                                  var c = _clientFactory.CreateClient();
                                                  var b = await c.GetByteArrayAsync(p.Thumbnail, inner);
                                                  model.Thumbnail = Bitmap.DecodeToWidth(new MemoryStream(b), 64);
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
                                          catch (OperationCanceledException) { }
                                          catch (Exception ex)
                                          {
                                              _notificationService.PopMessage($"{purl}: {ex.Message}",
                                                                              "Failed to parse purl",
                                                                              NotificationLevel.Warning);
                                          }
                                          finally
                                          {
                                              semaphore.Release();
                                          }
                                  },
                                  inner);
            return model;
        }

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
                if (package.Thumbnail is not null)
                {
                    using var client = _clientFactory.CreateClient();
                    var bytes = await client.GetByteArrayAsync(package.Thumbnail, inner);
                    thumbnail = Bitmap.DecodeToWidth(new MemoryStream(bytes), 64);
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
    }

    protected override void OnUpdateModel(string key, Profile profile)
    {
        if (profile.Setup.Loader is not null && LoaderHelper.TryParse(profile.Setup.Loader, out var result))
            LoaderLabel = LoaderHelper.ToDisplayLabel(result.Identity, result.Version);
        else
            LoaderLabel = "None";

        _updatingSubscription?.Dispose();
        UpdatingPending = true;
        UpdatingProgress = 0;

        base.OnUpdateModel(key, profile);
    }

    protected override async Task OnInitializedAsync(CancellationToken token)
    {
        _pageCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (!InstanceManager.IsInUse(Basic.Key))
            TriggerRefresh(_pageCancellationTokenSource.Token);

        await base.OnInitializedAsync(token);
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        _pageCancellationTokenSource?.Cancel();
        _updatingSubscription?.Dispose();

        return base.OnDeinitializeAsync(token);
    }

    protected override void OnInstanceUpdating(UpdateTracker tracker)
    {
        _updatingCancellationTokenSource?.Cancel();
        TrackUpdateProgress(tracker);
        base.OnInstanceUpdating(tracker);
    }

    protected override void OnInstanceUpdated(UpdateTracker tracker)
    {
        ArgumentNullException.ThrowIfNull(_pageCancellationTokenSource);
        TriggerRefresh(_pageCancellationTokenSource.Token);
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
    public partial InstanceReferenceModel? Reference { get; set; }

    [ObservableProperty]
    public partial string LoaderLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SourceCache<InstancePackageModel, string> Stage { get; set; } = new(x => x.Purl);

    [ObservableProperty]
    public partial SourceCache<InstancePackageModel, string> Stash { get; set; } = new(x => x.Purl);

    [ObservableProperty]
    public partial AvaloniaList<InstancePackageModel> Draft { get; set; } = [];

    [ObservableProperty]
    public partial int StageCount { get; set; }

    [ObservableProperty]
    public partial int StashCount { get; set; }

    [ObservableProperty]
    public partial double UpdatingProgress { get; set; }

    [ObservableProperty]
    public partial bool UpdatingPending { get; set; } = true;

    #endregion
}