using CsvHelper;
using DotNext.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Trident.Abstractions.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static System.Environment;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Dialogs
{
    public sealed partial class SaveFileDialog
    {


        public string Directory
        {
            get { return (string)GetValue(DirectoryProperty); }
            set { SetValue(DirectoryProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Directory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DirectoryProperty =
            DependencyProperty.Register("Directory", typeof(string), typeof(SaveFileDialog), new PropertyMetadata(GetFolderPath(SpecialFolder.MyDocuments)));



        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register("FileName", typeof(string), typeof(SaveFileDialog), new PropertyMetadata(string.Empty));


        public SaveFileDialog(XamlRoot root)
        {
            XamlRoot = root;
            InitializeComponent();
        }

        private async void PickFilePathButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileSavePicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Current.Window));
            new Dictionary<string, IList<string>>()
            {
                { "Plain Text", new List<string>(){ ".txt"} }
            }.ForEach(picker.FileTypeChoices.Add);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = FileName;
            var file = await picker.PickSaveFileAsync();
            if (file != null && file.Path != null)
            {
                Directory = Path.GetDirectoryName(file.Path) ?? string.Empty;
                FileName = Path.GetFileName(file.Path);
            }
        }
    }
}