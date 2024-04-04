// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Polymerium.App.Dialogs
{
    public sealed partial class DragDropInputDialog
    {
        public static readonly DependencyProperty CaptionTextProperty = DependencyProperty.Register(nameof(CaptionText),
            typeof(string), typeof(DragDropInputDialog), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty BodyTextProperty = DependencyProperty.Register(nameof(BodyText),
            typeof(string), typeof(DragDropInputDialog), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconGlyphProperty = DependencyProperty.Register(nameof(IconGlyph),
            typeof(string), typeof(DragDropInputDialog), new PropertyMetadata("\uE7C9"));

        public static readonly DependencyProperty ResultPathProperty = DependencyProperty.Register(nameof(ResultPath),
            typeof(string), typeof(DragDropInputDialog), new PropertyMetadata(string.Empty));

        public DragDropInputDialog(XamlRoot root)
        {
            XamlRoot = root;
            InitializeComponent();
        }

        public string CaptionText
        {
            get => (string)GetValue(CaptionTextProperty);
            set => SetValue(CaptionTextProperty, value);
        }

        public string BodyText
        {
            get => (string)GetValue(BodyTextProperty);
            set => SetValue(BodyTextProperty, value);
        }

        public string IconGlyph
        {
            get => (string)GetValue(IconGlyphProperty);
            set => SetValue(IconGlyphProperty, value);
        }

        public string ResultPath
        {
            get => (string)GetValue(ResultPathProperty);
            set => SetValue(ResultPathProperty, value);
        }

        private async void UIElement_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var files = await e.DataView.GetStorageItemsAsync();
                var file = files.FirstOrDefault();
                ResultPath = file?.Path ?? ResultPath;
                e.Handled = true;
            }
        }

        private async void ChooseButton_OnClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new();
            InitializeWithWindow.Initialize(picker,
                WindowNative.GetWindowHandle(App.Current.Window));
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            var file = await picker.PickSingleFileAsync();
            ResultPath = file?.Path ?? ResultPath;
        }

        private void UIElement_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Link;
            }
        }
    }
}