using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;

namespace Polymerium.Avalonia.ToastModels;

public partial class ExhibitModpackToastModel(
    IViewContext<ExhibitModpackToastModel.Parameter> context,
    DataService dataService,
    PersistenceService persistenceService) : ViewModelBase
{
    private readonly Parameter _parameter = context.GetRequiredParameter();
    private CancellationTokenSource? _galleryThumbnailCancellationTokenSource;

    #region Nested type: Parameter

    public sealed record Parameter(
        ExhibitModpackModel Modpack,
        Uri? FallbackThumbnail,
        ICommand InstallCommand);

    #endregion

    #region Direct

    public ExhibitModpackModel Modpack => _parameter.Modpack;
    public Uri? Thumbnail => Modpack.Thumbnail ?? _parameter.FallbackThumbnail;
    public Uri? HeroImage => Modpack.Gallery.FirstOrDefault()?.Uri ?? Thumbnail;
    public ICommand InstallCommand => _parameter.InstallCommand;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    [ObservableProperty]
    public partial ExhibitVersionModel? SelectedVersion { get; set; }

    [ObservableProperty]
    public partial LazyObject? LazyVersions { get; set; }

    [ObservableProperty]
    public partial LazyObject? LazyDescription { get; set; }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        IsFavorite = persistenceService.IsFavoriteProject(Modpack.Label, Modpack.Namespace, Modpack.ProjectId);
        LazyVersions = ConstructVersions();
        LazyDescription = ConstructDescription();
        _galleryThumbnailCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        _ = LoadGalleryThumbnailsAsync(_galleryThumbnailCancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        LazyVersions?.Cancel();
        LazyDescription?.Cancel();
        _galleryThumbnailCancellationTokenSource?.Cancel();
        _galleryThumbnailCancellationTokenSource?.Dispose();
        _galleryThumbnailCancellationTokenSource = null;
        return Task.CompletedTask;
    }

    #endregion

    private async Task LoadGalleryThumbnailsAsync(CancellationToken token)
    {
        foreach (var image in Modpack.Gallery)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var thumbnail = await dataService.GetBitmapAsync(image.Uri, 112);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                image.ThumbnailBitmap = thumbnail;
            }
            catch
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }

    #region Lazy construction

    private LazyObject ConstructVersions() =>
        new(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var versions = await dataService.InspectVersionsAsync(Modpack.Label,
                                                                  Modpack.Namespace,
                                                                  Modpack.ProjectId,
                                                                  Filter.None with { Kind = ResourceKind.Modpack });
            var models = versions
                        .Select(x => new ExhibitVersionModel(Modpack.Label,
                                                             Modpack.Namespace,
                                                             Modpack.ProjectName,
                                                             Modpack.ProjectId,
                                                             x.VersionName,
                                                             x.VersionId,
                                                             string.Join(",",
                                                                         x.Requirements.AnyOfLoaders.Select(LoaderHelper
                                                                            .ToDisplayName)),
                                                             string.Join(",", x.Requirements.AnyOfVersions),
                                                             string.Empty,
                                                             x.PublishedAt,
                                                             x.DownloadCount,
                                                             x.ReleaseType,
                                                             PackageHelper.ToPref(x.Label,
                                                                                  x.Namespace,
                                                                                  x.ProjectId,
                                                                                  x.VersionId)))
                        .ToList();
            SelectedVersion = models.FirstOrDefault();
            return new ExhibitVersionCollection(models);
        });

    private LazyObject ConstructDescription() =>
        new(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            return await dataService.ReadDescriptionAsync(new(Modpack.Label, Modpack.Namespace, Modpack.ProjectId));
        });

    #endregion

    #region Commands

    [RelayCommand]
    private void Favorite()
    {
        if (IsFavorite)
        {
            persistenceService.RemoveFavoriteProject(Modpack.Label, Modpack.Namespace, Modpack.ProjectId);
            IsFavorite = false;
            return;
        }

        persistenceService.AddFavoriteProject(Modpack.Label,
                                              Modpack.Namespace,
                                              Modpack.ProjectId,
                                              Modpack.ProjectName,
                                              Modpack.AuthorName,
                                              Modpack.Summary,
                                              Modpack.Reference,
                                              Modpack.Thumbnail,
                                              ResourceKind.Modpack,
                                              Modpack.DownloadCount,
                                              Modpack.Tags,
                                              Modpack.CreatedAt,
                                              Modpack.UpdatedAt);
        IsFavorite = true;
    }

    [RelayCommand]
    private Task NavigateUri(Uri? uri)
    {
        if (uri is not null)
        {
            return TopLevelHelper.LaunchUriAsync(TopLevelHelper.GetTopLevel(),
                                                 uri,
                                                 LanguageManager.Instance.ExhibitModpackToast_OpenModpackLinkDangerNotificationTitle.Current());
        }

        return Task.CompletedTask;
    }

    #endregion
}
