using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Trident.Abstractions;

namespace Polymerium.App.Modals;

public partial class JarInJarScannerWidgetModal : Modal
{
    public JarInJarScannerWidgetModal() => InitializeComponent();

    #region Properties

    public required string Key { get; init; }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task ScanAsync()
    {
        var folder = Path.Combine(PathDef.Default.DirectoryOfImport(Key), "mods");
        if (Directory.Exists(folder))
        {
            var files = Directory.GetFiles(folder, "*.jar", SearchOption.TopDirectoryOnly);
        }
        // return Empty result set
    }

    #endregion
}
