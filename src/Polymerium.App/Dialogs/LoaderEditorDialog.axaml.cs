using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Dialogs;

public partial class LoaderEditorDialog : Dialog
{
    public required OverlayService OverlayService { get; init; }
    public required DataService DataService { get; init; }

    public LoaderEditorDialog()
    {
        InitializeComponent();
    }

    public static readonly DirectProperty<LoaderEditorDialog, LoaderCandidateModel?> LoaderProperty =
        AvaloniaProperty.RegisterDirect<LoaderEditorDialog, LoaderCandidateModel?>(nameof(Loader),
            o => o.Loader,
            (o, v) => o.Loader = v);

    public LoaderCandidateModel? Loader
    {
        get;
        set => SetAndRaise(LoaderProperty, ref field, value);
    }

    public static readonly DirectProperty<LoaderEditorDialog, LazyObject?> LazyVersionsProperty =
        AvaloniaProperty.RegisterDirect<LoaderEditorDialog, LazyObject?>(nameof(LazyVersions),
                                                                         o => o.LazyVersions,
                                                                         (o, v) => o.LazyVersions = v);

    public LazyObject? LazyVersions
    {
        get;
        set => SetAndRaise(LazyVersionsProperty, ref field, value);
    }

    public static readonly DirectProperty<LoaderEditorDialog, LoaderCandidateVersionModel?> SelectedVersionProperty =
        AvaloniaProperty.RegisterDirect<LoaderEditorDialog, LoaderCandidateVersionModel?>(nameof(SelectedVersion),
            o => o.SelectedVersion,
            (o, v) => o.SelectedVersion = v);

    public LoaderCandidateVersionModel? SelectedVersion
    {
        get;
        set => SetAndRaise(SelectedVersionProperty, ref field, value);
    }


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        LoadVersions();
    }

    private void LoadVersions()
    {
        if (Loader != null)
        {
            var lazy = new LazyObject(async token =>
                                      {
                                          var index = await DataService.GetComponentAsync(Loader.Id);
                                          return new LoaderCandidateVersionCollectionModel(index
                                             .Versions.OrderByDescending(x => x.ReleaseTime)
                                             .Select(x => new LoaderCandidateVersionModel(x.Version))
                                             .ToList());
                                      },
                                      CancellationToken.None);
            LazyVersions = lazy;
        }
    }


    private async void AddLoaderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new LoaderPickerDialog
        {
            Candidates =
            [
                new LoaderCandidateModel(LoaderHelper.LOADERID_FORGE,
                                         "Forge",
                                         AssetUriIndex.LOADER_FORGE_BITMAP),
                new LoaderCandidateModel(LoaderHelper.LOADERID_NEOFORGE,
                                         "NeoForge",
                                         AssetUriIndex.LOADER_NEOFORGE_BITMAP),
                new LoaderCandidateModel(LoaderHelper.LOADERID_FABRIC,
                                         "Fabric",
                                         AssetUriIndex.LOADER_FABRIC_BITMAP),
                new LoaderCandidateModel(LoaderHelper.LOADERID_QUILT,
                                         "Quilt",
                                         AssetUriIndex.LOADER_QUILT_BITMAP)
            ]
        };
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is LoaderCandidateModel model)
        {
            Loader = model;
            LoadVersions();
        }
        else
            Loader = null;
    }
}