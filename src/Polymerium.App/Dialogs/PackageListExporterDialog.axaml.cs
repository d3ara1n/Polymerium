using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs;

public partial class PackageListExporterDialog : Dialog
{
    public static readonly DirectProperty<PackageListExporterDialog, int> PackageCountProperty =
        AvaloniaProperty.RegisterDirect<PackageListExporterDialog, int>(nameof(PackageCount),
            o => o.PackageCount,
            (o, v) => o.PackageCount = v);

    public PackageListExporterDialog() => InitializeComponent();

    public int PackageCount
    {
        get;
        set => SetAndRaise(PackageCountProperty, ref field, value);
    }


    protected override bool ValidateResult(object? result)
    {
        if (result is string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null && Directory.Exists(dir)) return true;
        }

        return false;
    }

    #region Commands

    [RelayCommand]
    private async Task Browse()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
        {
            var storage = top.StorageProvider;
            if (storage.CanOpen)
            {
                var file = await storage.SaveFilePickerAsync(new()
                {
                    SuggestedStartLocation =
                        await storage
                            .TryGetWellKnownFolderAsync(WellKnownFolder
                                .Downloads),
                    SuggestedFileName = "packages.csv",
                    DefaultExtension = "csv"
                });
                if (file != null) Result = file.TryGetLocalPath();
            }
        }
    }

    #endregion
}