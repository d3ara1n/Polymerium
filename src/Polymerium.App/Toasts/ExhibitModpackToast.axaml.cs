using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Toasts;

public partial class ExhibitModpackToast : Toast
{
    public static readonly StyledProperty<IRelayCommand<ExhibitVersionModel>?> InstallCommandProperty =
        AvaloniaProperty.Register<ExhibitModpackToast, IRelayCommand<ExhibitVersionModel>?>(nameof(InstallCommand));

    public static readonly DirectProperty<ExhibitModpackToast, LazyObject?> LazyDescriptionProperty =
        AvaloniaProperty.RegisterDirect<ExhibitModpackToast, LazyObject?>(nameof(LazyDescription),
                                                                          o => o.LazyDescription,
                                                                          (o, v) => o.LazyDescription = v);

    public static readonly DirectProperty<ExhibitModpackToast, LazyObject?> LazyVersionsProperty =
        AvaloniaProperty.RegisterDirect<ExhibitModpackToast, LazyObject?>(nameof(LazyVersions),
                                                                          o => o.LazyVersions,
                                                                          (o, v) => o.LazyVersions = v);

    public ExhibitModpackToast() => InitializeComponent();

    public IRelayCommand<ExhibitVersionModel>? InstallCommand
    {
        get => GetValue(InstallCommandProperty);
        set => SetValue(InstallCommandProperty, value);
    }

    public required DataService DataService { get; set; }

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

        LoadVersions();
        LoadDescription();
    }

    private void LoadVersions() =>
        LazyVersions = new(async _ =>
        {
            var project = Modpack;
            var versions = await DataService.InspectVersionsAsync(project.Label,
                                                                  project.Namespace,
                                                                  project.ProjectId,
                                                                  Filter.None with { Kind = ResourceKind.Modpack });
            return new ExhibitVersionCollection([
                .. versions.Select(x => new ExhibitVersionModel(project.Label,
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
            ]);
        });

    private void LoadDescription() =>
        LazyDescription = new(async _ =>
        {
            var description =
                await DataService.ReadDescriptionAsync(Modpack.Label, Modpack.Namespace, Modpack.ProjectId);
            return description;
        });

    #region Commands

    [RelayCommand]
    private void NavigateUri(string? url)
    {
        if (url is not null)
        {
            var rev = new Uri(url, UriKind.RelativeOrAbsolute);
            TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(rev.IsAbsoluteUri ? rev : new(Modpack.Reference, rev));
        }
    }

    #endregion
}
