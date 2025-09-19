using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.Repositories;

namespace Polymerium.App.Modals;

public partial class AssetImporterDialog : Dialog
{
    public AssetImporterDialog()
    {
        InitializeComponent();
    }

    public required DataService DataService { get; init; }
    public required NotificationService NotificationService { get; init; }

    public static readonly DirectProperty<AssetImporterDialog, string?> PathAcceptedProperty =
        AvaloniaProperty.RegisterDirect<AssetImporterDialog, string?>(nameof(PathAccepted),
                                                                      o => o.PathAccepted,
                                                                      (o, v) => o.PathAccepted = v);

    public static readonly DirectProperty<AssetImporterDialog, bool> IsAssetSelectedProperty =
        AvaloniaProperty.RegisterDirect<AssetImporterDialog, bool>(nameof(IsAssetSelected),
                                                                   o => o.IsAssetSelected,
                                                                   (o, v) => o.IsAssetSelected = v);

    public bool IsAssetSelected
    {
        get;
        set
        {
            if (SetAndRaise(IsAssetSelectedProperty, ref field, value) && value)
            {
                Result = Model?.Persist;
                IsPackageSelected = false;
            }
        }
    }

    public static readonly DirectProperty<AssetImporterDialog, bool> IsPackageSelectedProperty =
        AvaloniaProperty.RegisterDirect<AssetImporterDialog, bool>(nameof(IsPackageSelected),
                                                                   o => o.IsPackageSelected,
                                                                   (o, v) => o.IsPackageSelected = v);

    public bool IsPackageSelected
    {
        get;
        set
        {
            if (SetAndRaise(IsPackageSelectedProperty, ref field, value) && value)
            {
                Result = Model?.Package;
                IsAssetSelected = false;
            }
        }
    }

    public string? PathAccepted
    {
        get;
        set
        {
            if (SetAndRaise(PathAcceptedProperty, ref field, value) && !string.IsNullOrEmpty(value))
            {
                Task.Run(() => OnPathAccepted(value));
            }
        }
    }

    public static readonly DirectProperty<AssetImporterDialog, AssetIdentificationModel?> ModelProperty =
        AvaloniaProperty.RegisterDirect<AssetImporterDialog, AssetIdentificationModel?>(nameof(Model),
            o => o.Model,
            (o, v) => o.Model = v);

    public AssetIdentificationModel? Model
    {
        get;
        set => SetAndRaise(ModelProperty, ref field, value);
    }

    private void DropZone_OnDragOver(object? sender, DropZone.DragOverEventArgs e)
    {
        e.Handled = true;
        if (e.Data.Contains(DataFormats.Files))
        {
            e.Accepted = true;
        }
    }

    private void DropZone_OnDrop(object? sender, DropZone.DropEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.Handled = true;
            var file = e.Data.GetFiles()?.FirstOrDefault();
            if (file != null)
            {
                e.Model = file.TryGetLocalPath();
            }
        }
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
        {
            var storage = top.StorageProvider;
            if (storage.CanOpen)
            {
                var files = await storage.OpenFilePickerAsync(new());
                var file = files.FirstOrDefault();
                if (file != null)
                {
                    PathAccepted = file.TryGetLocalPath();
                }
            }
        }
    }

    private async Task OnPathAccepted(string path)
    {
        try
        {
            var model = await TryIdentityFileAsync(path);
            Dispatcher.UIThread.Post(() => Model = model);
        }
        catch (ResourceNotFoundException)
        {
            Dispatcher.UIThread.Post(() => Model = null);
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => NotificationService.PopMessage(ex, "Failed to identify file"));
        }
    }

    private async Task<AssetIdentificationModel> TryIdentityFileAsync(string? path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found", path);
        }

        try
        {
            var package = await DataService.IdentifyVersionAsync(path);
            return new(new(package), new(path));
        }
        catch (ResourceNotFoundException)
        {
            return new(null, new(path));
        }
    }

    protected override bool ValidateResult(object? result) =>
        result is AssetIdentificationPackageModel or AssetIdentificationPersistModel;
}
