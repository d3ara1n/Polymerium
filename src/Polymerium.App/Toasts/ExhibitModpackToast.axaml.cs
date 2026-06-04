using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Utilities;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using AppResources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.Toasts;

public partial class ExhibitModpackToast : Toast
{
    public static readonly StyledProperty<IRelayCommand<ExhibitVersionModel>?> InstallCommandProperty =
        AvaloniaProperty.Register<ExhibitModpackToast, IRelayCommand<ExhibitVersionModel>?>(
            nameof(InstallCommand)
        );

    public static readonly DirectProperty<
        ExhibitModpackToast,
        LazyObject?
    > LazyDescriptionProperty = AvaloniaProperty.RegisterDirect<ExhibitModpackToast, LazyObject?>(
        nameof(LazyDescription),
        o => o.LazyDescription,
        (o, v) => o.LazyDescription = v
    );

    public static readonly DirectProperty<ExhibitModpackToast, LazyObject?> LazyVersionsProperty =
        AvaloniaProperty.RegisterDirect<ExhibitModpackToast, LazyObject?>(
            nameof(LazyVersions),
            o => o.LazyVersions,
            (o, v) => o.LazyVersions = v
        );

    public static readonly DirectProperty<ExhibitModpackToast, bool> IsFavoriteProperty =
        AvaloniaProperty.RegisterDirect<ExhibitModpackToast, bool>(
            nameof(IsFavorite),
            o => o.IsFavorite,
            (o, v) => o.IsFavorite = v
        );

    public static readonly DirectProperty<
        ExhibitModpackToast,
        ExhibitVersionModel?
    > SelectedVersionProperty = AvaloniaProperty.RegisterDirect<
        ExhibitModpackToast,
        ExhibitVersionModel?
    >(nameof(SelectedVersion), o => o.SelectedVersion, (o, v) => o.SelectedVersion = v);

    public ExhibitModpackToast() => InitializeComponent();

    public ExhibitVersionModel? SelectedVersion
    {
        get;
        set => SetAndRaise(SelectedVersionProperty, ref field, value);
    }

    public IRelayCommand<ExhibitVersionModel>? InstallCommand
    {
        get => GetValue(InstallCommandProperty);
        set => SetValue(InstallCommandProperty, value);
    }

    public bool IsFavorite
    {
        get;
        set => SetAndRaise(IsFavoriteProperty, ref field, value);
    }

    public required DataService DataService { get; set; }
    public required PersistenceService PersistenceService { get; set; }

    public LazyObject? LazyVersions
    {
        get;
        set => SetAndRaise(LazyVersionsProperty, ref field, value);
    }

    private ExhibitModpackModel Modpack => (ExhibitModpackModel)DataContext!;

    public LazyObject? LazyDescription
    {
        get;
        set => SetAndRaise(LazyDescriptionProperty, ref field, value);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        IsFavorite = PersistenceService.IsFavoriteProject(
            Modpack.Label,
            Modpack.Namespace,
            Modpack.ProjectId
        );
        LoadVersions();
        LoadDescription();
    }

    private void LoadVersions() =>
        LazyVersions = new(async _ =>
        {
            var project = Modpack;
            var versions = await DataService.InspectVersionsAsync(
                project.Label,
                project.Namespace,
                project.ProjectId,
                Filter.None with
                {
                    Kind = ResourceKind.MODPACK,
                }
            );
            var rv = versions
                .Select(x => new ExhibitVersionModel(
                    project.Label,
                    project.Namespace,
                    project.ProjectName,
                    project.ProjectId,
                    x.VersionName,
                    x.VersionId,
                    string.Join(
                        ",",
                        x.Requirements.AnyOfLoaders.Select(LoaderHelper.ToDisplayName)
                    ),
                    string.Join(",", x.Requirements.AnyOfVersions),
                    string.Empty,
                    x.PublishedAt,
                    x.DownloadCount,
                    x.ReleaseType,
                    PackageHelper.ToPurl(x.Label, x.Namespace, x.ProjectId, x.VersionId)
                ))
                .ToList();
            SelectedVersion = rv.FirstOrDefault();
            return new ExhibitVersionCollection(rv);
        });

    private void LoadDescription() =>
        LazyDescription = new(async _ =>
        {
            var description = await DataService.ReadDescriptionAsync(
                Modpack.Label,
                Modpack.Namespace,
                Modpack.ProjectId
            );
            return description;
        });

    #region Commands

    [RelayCommand]
    private void Favorite()
    {
        if (IsFavorite)
        {
            PersistenceService.RemoveFavoriteProject(Modpack.Label, Modpack.Namespace, Modpack.ProjectId);
            IsFavorite = false;
            return;
        }

        PersistenceService.AddFavoriteProject(
            Modpack.Label,
            Modpack.Namespace,
            Modpack.ProjectId,
            Modpack.ProjectName,
            Modpack.AuthorName,
            Modpack.Summary,
            Modpack.Reference,
            Modpack.Thumbnail,
            ResourceKind.MODPACK,
            Modpack.DownloadCountRaw,
            Modpack.Tags,
            Modpack.UpdatedAtRaw,
            Modpack.UpdatedAtRaw
        );
        IsFavorite = true;
    }

    [RelayCommand]
    private Task NavigateUri(string? url)
    {
        if (url is not null)
        {
            var rev = new Uri(url, UriKind.RelativeOrAbsolute);
            return TopLevelHelper.LaunchUriAsync(
                TopLevel.GetTopLevel(this),
                rev.IsAbsoluteUri ? rev : new(Modpack.Reference, rev),
                AppResources.ExhibitModpackToast_OpenModpackLinkDangerNotificationTitle
            );
        }

        return Task.CompletedTask;
    }

    #endregion
}
