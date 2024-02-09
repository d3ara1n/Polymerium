// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MimeDetective;
using MimeDetective.Storage;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs
{
    public sealed partial class CreateProfileDialog
    {
        private static readonly ContentInspector INSPECTOR = (new ContentInspectorBuilder()
        {
            Definitions = MimeDetective.Definitions.Default.All()
        }).Build();

        // https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.image?view=windows-app-sdk-1.4
        private static readonly string[] SUPPORTED_MIMES =
        [
            "image/jpeg",
            "image/png",
            "image/bmp",
            "image/gif",
            "image/tiff",
            "image/vnd.microsoft.icon",
        ];

        public BitmapImage Image { get; } = new();
        public MinecraftVersionModel[] Versions { get; }

        private MemoryStream? stream;

        public CreateProfileDialog(XamlRoot root, IEnumerable<MinecraftVersionModel> versions)
        {
            XamlRoot = root;
            Versions = versions.ToArray();
            InitializeComponent();
        }

        private void UIElement_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems) ||
                e.DataView.Contains(StandardDataFormats.Bitmap))
                e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void UIElement_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var files = await e.DataView.GetStorageItemsAsync();
                var first = files.FirstOrDefault();
                if (first != null && File.Exists(first.Path) && first.IsOfType(StorageItemTypes.File))
                {
                    await using var reader = File.OpenRead(first.Path);
                    var results = INSPECTOR.Inspect(reader).ByMimeType();
                    if (results.Any(x => SUPPORTED_MIMES.Contains(x.MimeType)))
                    {
                        reader.Position = 0;
                        await SetBitmapAsync(reader);
                    }
                }
            }
            else if (e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                var bitmap = await e.DataView.GetBitmapAsync();
                await using var reader = (await bitmap.OpenReadAsync()).AsStreamForRead();
                await SetBitmapAsync(reader);
            }
        }

        private async Task SetBitmapAsync(Stream reader)
        {
            if (stream != null) await stream.DisposeAsync();
            stream = new MemoryStream();
            await reader.CopyToAsync(stream);
            stream.Position = 0;
            await Image.SetSourceAsync(stream.AsRandomAccessStream());
        }

        private void VersionBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is MinecraftVersionModel version)
            {
                VersionBox.Text = version.Version;
            }
        }
    }
}