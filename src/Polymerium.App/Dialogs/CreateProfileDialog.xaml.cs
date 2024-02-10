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
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MimeDetective;
using MimeDetective.Storage;
using Polymerium.App.Models;
using CommunityToolkit.WinUI.UI;
using DotNext.Collections.Generic;
using Trident.Abstractions.Resources;
using WinRT.Interop;

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
        public AdvancedCollectionView Versions { get; }

        private MemoryStream? stream;
        private string filter = string.Empty;

        public static readonly DependencyProperty SelectedVersionProperty = DependencyProperty.Register(
            nameof(SelectedVersion), typeof(string), typeof(CreateProfileDialog),
            new PropertyMetadata(string.Empty));

        public string SelectedVersion
        {
            get => (string)GetValue(SelectedVersionProperty);
            set => SetValue(SelectedVersionProperty, value);
        }

        public static readonly DependencyProperty IsReleaseChosenProperty = DependencyProperty.Register(
            nameof(IsReleaseChosen), typeof(bool), typeof(CreateProfileDialog), new PropertyMetadata(true));

        public bool IsReleaseChosen
        {
            get => (bool)GetValue(IsReleaseChosenProperty);
            set => SetValue(IsReleaseChosenProperty, value);
        }

        public static readonly DependencyProperty IsSnapshotChosenProperty = DependencyProperty.Register(
            nameof(IsSnapshotChosen), typeof(bool), typeof(CreateProfileDialog), new PropertyMetadata(true));

        public bool IsSnapshotChosen
        {
            get => (bool)GetValue(IsSnapshotChosenProperty);
            set => SetValue(IsSnapshotChosenProperty, value);
        }

        public static readonly DependencyProperty IsExperimentChosenProperty = DependencyProperty.Register(
            nameof(IsExperimentChosen), typeof(bool), typeof(CreateProfileDialog), new PropertyMetadata(true));

        public bool IsExperimentChosen
        {
            get => (bool)GetValue(IsExperimentChosenProperty);
            set => SetValue(IsExperimentChosenProperty, value);
        }

        public static readonly DependencyProperty IsLegacyChosenProperty = DependencyProperty.Register(
            nameof(IsLegacyChosen), typeof(bool), typeof(CreateProfileDialog), new PropertyMetadata(true));

        public bool IsLegacyChosen
        {
            get => (bool)GetValue(IsLegacyChosenProperty);
            set => SetValue(IsLegacyChosenProperty, value);
        }

        public MemoryStream? ThumbnailImage => stream;

        public string InstanceName =>
            !string.IsNullOrEmpty(InstanceNameBox.Text) ? InstanceNameBox.Text : SelectedVersion;

        public CreateProfileDialog(XamlRoot root, IEnumerable<MinecraftVersionModel> versions)
        {
            XamlRoot = root;
            var array = versions.ToList();
            InitializeComponent();
            Versions = new(array) { Filter = Filter };
            Versions.SortDescriptions.Add(new SortDescription(nameof(MinecraftVersionModel.ReleasedAt),
                SortDirection.Descending));
            var def = array.Where(x => x.Type == ReleaseType.Release).MaxBy(x => x.ReleasedAt);
            if (def != null)
                SelectedVersion = def.Version;
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

        private void VersionBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                filter = VersionBox.Text;
                Versions.Refresh();
            }
        }

        private bool Filter(object obj)
        {
            if (obj is MinecraftVersionModel version)
                return version.Type switch
                {
                    ReleaseType.Release => IsReleaseChosen,
                    ReleaseType.Snapshot => IsSnapshotChosen,
                    ReleaseType.Experiment => IsExperimentChosen,
                    ReleaseType.Alpha or ReleaseType.Beta => IsLegacyChosen,
                    _ => true
                } && (string.IsNullOrEmpty(filter) ||
                      version.Version.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
            return false;
        }

        private async void PickFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            InitializeWithWindow.Initialize(picker,
                WindowNative.GetWindowHandle(App.Current.Window));
            new[] { ".jpg", ".jpeg", ".bmp", ".png", ".gif" }.ForEach(picker.FileTypeFilter.Add);
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            var file = await picker.PickSingleFileAsync();
            if (file != null && file.Path != null && File.Exists(file.Path))
            {
                await using var reader = File.OpenRead(file.Path);
                var results = INSPECTOR.Inspect(reader).ByMimeType();
                if (results.Any(x => SUPPORTED_MIMES.Contains(x.MimeType)))
                {
                    reader.Position = 0;
                    await SetBitmapAsync(reader);
                }
            }
        }
    }
}