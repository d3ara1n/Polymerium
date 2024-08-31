using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static System.Environment;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Dialogs;

public sealed partial class SaveFileDialog
{
    // Using a DependencyProperty as the backing store for Directory.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DirectoryProperty =
        DependencyProperty.Register(nameof(Directory), typeof(string), typeof(SaveFileDialog),
            new PropertyMetadata(GetFolderPath(SpecialFolder.MyDocuments)));

    // Using a DependencyProperty as the backing store for FileName.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty FileNameProperty =
        DependencyProperty.Register(nameof(FileName), typeof(string), typeof(SaveFileDialog),
            new PropertyMetadata(string.Empty));


    public SaveFileDialog(XamlRoot root)
    {
        XamlRoot = root;
        InitializeComponent();
    }


    public string Directory
    {
        get => (string)GetValue(DirectoryProperty);
        set => SetValue(DirectoryProperty, value);
    }


    public string FileName
    {
        get => (string)GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }

    private async void PickFilePathButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Current.Window));
        foreach (var item in new Dictionary<string, IList<string>> { { "Plain Text", new List<string> { ".txt" } } })
            picker.FileTypeChoices.Add(item);
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