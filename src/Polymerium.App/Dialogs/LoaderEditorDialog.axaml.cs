using System.Linq;
using Avalonia;
using Avalonia.Interactivity;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Dialogs;

public partial class LoaderEditorDialog : Dialog
{
    private static readonly LoaderCandidateModel[] Candidates =
    [
        new(LoaderHelper.LOADERID_NEOFORGE, "NeoForge", AssetUriIndex.LOADER_NEOFORGE_BITMAP),
        new(LoaderHelper.LOADERID_FORGE, "Forge", AssetUriIndex.LOADER_FORGE_BITMAP),
        new(LoaderHelper.LOADERID_FABRIC, "Fabric", AssetUriIndex.LOADER_FABRIC_BITMAP),
        new(LoaderHelper.LOADERID_QUILT, "Quilt", AssetUriIndex.LOADER_QUILT_BITMAP)
    ];

    public static readonly DirectProperty<LoaderEditorDialog, string?> SelectedLoaderProperty =
        AvaloniaProperty.RegisterDirect<LoaderEditorDialog, string?>(nameof(SelectedLoader),
                                                                     o => o.SelectedLoader,
                                                                     (o, v) => o.SelectedLoader = v);

    public static readonly DirectProperty<LoaderEditorDialog, LazyObject?> LazyVersionsProperty =
        AvaloniaProperty.RegisterDirect<LoaderEditorDialog, LazyObject?>(nameof(LazyVersions),
                                                                         o => o.LazyVersions,
                                                                         (o, v) => o.LazyVersions = v);

    public static readonly DirectProperty<LoaderEditorDialog, string?> SelectedVersionProperty =
        AvaloniaProperty.RegisterDirect<LoaderEditorDialog, string?>(nameof(SelectedVersion),
                                                                     o => o.SelectedVersion,
                                                                     (o, v) => o.SelectedVersion = v);

    public static readonly DirectProperty<LoaderEditorDialog, LoaderCandidateModel?> LoaderProperty =
        AvaloniaProperty.RegisterDirect<LoaderEditorDialog, LoaderCandidateModel?>(nameof(Loader),
            o => o.Loader,
            (o, v) => o.Loader = v);

    public LoaderEditorDialog()
    {
        InitializeComponent();
    }

    public required OverlayService OverlayService { get; init; }
    public required DataService DataService { get; init; }
    public required string GameVersion { get; init; }

    public string? SelectedLoader
    {
        get;
        set => SetAndRaise(SelectedLoaderProperty, ref field, value);
    }


    public LazyObject? LazyVersions
    {
        get;
        set => SetAndRaise(LazyVersionsProperty, ref field, value);
    }

    public string? SelectedVersion
    {
        get;
        set => SetAndRaise(SelectedVersionProperty, ref field, value);
    }

    public LoaderCandidateModel? Loader
    {
        get;
        set => SetAndRaise(LoaderProperty, ref field, value);
    }


    private void LoadVersions()
    {
        if (SelectedLoader != null)
        {
            var lazy = new LazyObject(async token =>
            {
                if (token.IsCancellationRequested)
                    return null;
                var index = await DataService.GetComponentVersionsAsync(SelectedLoader, GameVersion);
                return new LoaderCandidateVersionCollectionModel([
                    .. index
                      .OrderByDescending(x => x.ReleaseTime)
                      .Select(x => new LoaderCandidateVersionModel(x.Version, x.Recommended))
                ]);
            });
            LazyVersions = lazy;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);


        if (change.Property == LoaderProperty)
            LoadVersions();

        if (change.Property == SelectedLoaderProperty)
        {
            var id = change.GetNewValue<string>();
            var model = Candidates.FirstOrDefault(x => x.Id == id);
            Loader = model;
        }

        if (change.Property == SelectedLoaderProperty || change.Property == SelectedVersionProperty)
        {
            if (SelectedLoader != null && SelectedVersion != null)
                Result = new LoaderCandidateSelectionModel(SelectedLoader, SelectedVersion);
            else
                Result = null;
        }
    }

    protected override bool ValidateResult(object? result) =>
        result is LoaderCandidateSelectionModel
     || (result is null && SelectedLoader == null && SelectedVersion == null);

    private async void AddLoaderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new LoaderPickerDialog { Candidates = Candidates };
        if (await OverlayService.PopDialogAsync(dialog) && dialog.Result is LoaderCandidateModel model)
            SelectedLoader = model.Id;
    }

    private void RemoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = null;
        SelectedVersion = null;
        SelectedLoader = null;
    }
}