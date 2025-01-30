using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    private ProfileGuard _owned;

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
        if (Basic.Source is not null && PackageHelper.TryParse(Basic.Source, out var result))
            try
            {
                using var client = _clientFactory.CreateClient();
                var package = await _repositories.ResolveAsync(result.Label, result.Namespace, result.Name,
                    result.Version,
                    Filter.Empty with
                    {
                        Kind = ResourceKind.Modpack
                    });
                Bitmap thumbnail;
                if (package.Thumbnail != null)
                {
                    var bytes = await client.GetByteArrayAsync(package.Thumbnail, token);
                    thumbnail = new Bitmap(new MemoryStream(bytes));
                }
                else
                {
                    thumbnail = new Bitmap(AssetLoader.Open(new Uri(AssetUriIndex.DIRT_IMAGE)));
                }

                Reference = new InstanceReferenceModel
                {
                    Name = package.ProjectName,
                    Thumbnail = thumbnail,
                    SourceUrl = package.Reference,
                    SourceLabel = package.Label
                };
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

    #endregion

    #region Command Handlers

    [RelayCommand]
    private void OpenSourceUrl(Uri? url)
    {
        if (url is not null) Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }

    #endregion
}