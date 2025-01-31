using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Polymerium.App.Assets;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceSetupViewModel : ViewModelBase
{
    #region Injected

    private readonly RepositoryAgent _repositories;
    private readonly IHttpClientFactory _clientFactory;

    #endregion

    private readonly ProfileGuard _owned;
    private CancellationTokenSource _cancellationTokenSource;
    internal const string DATAFORMATS = "never-gonna-give-you-up";

    public InstanceSetupViewModel(ViewBag bag, ProfileManager profileManager, RepositoryAgent repositories,
        IHttpClientFactory clientFactory)
    {
        _repositories = repositories;
        _clientFactory = clientFactory;
        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetMutable(key, out var mutable))
            {
                _owned = mutable;
                Basic = new InstanceBasicModel(key, mutable.Value.Name, mutable.Value.Setup.Version,
                    mutable.Value.Setup.Loader,
                    mutable.Value.Setup.Source);
                if (mutable.Value.Setup.Loader is not null &&
                    LoaderHelper.TryParse(mutable.Value.Setup.Loader, out var result))
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
            }
            else
            {
                throw new PageNotReachedException(typeof(InstanceView),
                    $"Key '{key}' is not valid instance or not found");
            }
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceSetupView), "Key to the instance is not provided");
        }
    }

    protected override async Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (Basic.Source is not null && PackageHelper.TryParse(Basic.Source, out var result))
            try
            {
                var package = await _repositories.ResolveAsync(result.Label, result.Namespace, result.Pid,
                    result.Vid,
                    Filter.Empty with
                    {
                        Kind = ResourceKind.Modpack
                    });

                Reference = new InstanceReferenceModel
                {
                    Name = package.ProjectName,
                    Thumbnail = package.Thumbnail,
                    SourceUrl = package.Reference,
                    SourceLabel = package.Label
                };

                InstancePackageModel Load(string purl)
                {
                    var model = new InstancePackageModel(purl);
                    model.Task = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), _cancellationTokenSource.Token);
                        if (PackageHelper.TryParse(model.Purl, out var v))
                            try
                            {
                                var p = await _repositories.ResolveAsync(v.Label, v.Namespace, v.Pid,
                                    v.Vid, Filter.Empty);
                                if (p.Thumbnail is not null)
                                {
                                    var c = _clientFactory.CreateClient();
                                    var b = await c.GetByteArrayAsync(p.Thumbnail, _cancellationTokenSource.Token);
                                    model.Thumbnail = new Bitmap(new MemoryStream(b));
                                }
                                else
                                {
                                    model.Thumbnail = new Bitmap(AssetLoader.Open(new Uri(AssetUriIndex.DIRT_IMAGE)));
                                }

                                model.Name = p.ProjectName;
                                model.Version = p.VersionName;
                                model.Summary = p.Summary;
                                model.Kind = p.Kind;
                                model.IsLoaded = true;
                            }
                            catch
                            {
                                // TODO
                            }
                    }, _cancellationTokenSource.Token);
                    return model;
                }

                var stages = _owned.Value.Setup.Stage.Select(Load).ToList();
                var stashes = _owned.Value.Setup.Stash.Select(Load).ToList();
                var drafts = _owned.Value.Setup.Draft.Select(Load).ToList();
                StageCount = stages.Count;
                StashCount = stashes.Count;
                Stage.AddOrUpdate(stages);
                Stash.AddOrUpdate(stashes);
                Draft.AddRange(drafts);
            }
            catch (ResourceNotFoundException ex)
            {
                // TODO: show a message about network unreachable or package url invalid
                Debug.WriteLine($"Resource not found: {ex.Message}");
            }
    }

    #region Rectives Models

    [ObservableProperty] private InstanceBasicModel _basic;
    [ObservableProperty] private InstanceReferenceModel? _reference;
    [ObservableProperty] private string _loaderLabel;
    [ObservableProperty] private SourceCache<InstancePackageModel, string> _stage = new(x => x.Purl);
    [ObservableProperty] private SourceCache<InstancePackageModel, string> _stash = new(x => x.Purl);
    [ObservableProperty] private AvaloniaList<InstancePackageModel> _draft = [];
    [ObservableProperty] private int _stageCount;
    [ObservableProperty] private int _stashCount;

    #endregion

    #region Command Handlers

    [RelayCommand]
    private void OpenSourceUrl(Uri? url)
    {
        if (url is not null) Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }

    #endregion
}