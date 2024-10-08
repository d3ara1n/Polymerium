// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Models;
using System.Collections.Generic;
using System.Linq;

namespace Polymerium.App.Dialogs;

public sealed partial class AddLoaderDialog
{
    // Using a DependencyProperty as the backing store for SelectedVersion.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedVersionProperty =
        DependencyProperty.Register(nameof(SelectedVersion), typeof(string), typeof(AddLoaderDialog),
            new PropertyMetadata(string.Empty));

    private string filter = string.Empty;


    public AddLoaderDialog(XamlRoot root, string identity, IEnumerable<LoaderVersionModel> versions)
    {
        XamlRoot = root;
        InitializeComponent();

        Versions =
            new AdvancedCollectionView(versions.OrderByDescending(x => x.ReleasedAt).ToList()) { Filter = Filter };
    }

    private AdvancedCollectionView Versions { get; }


    public string SelectedVersion
    {
        get => (string)GetValue(SelectedVersionProperty);
        set => SetValue(SelectedVersionProperty, value);
    }

    private bool Filter(object obj)
    {
        if (obj is LoaderVersionModel model)
        {
            return model.Version.Contains(filter);
        }

        return false;
    }

    private void VersionBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            filter = VersionBox.Text;
            Versions.Refresh();
        }
    }
}