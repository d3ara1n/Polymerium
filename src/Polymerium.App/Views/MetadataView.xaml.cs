using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;
using System;
using System.Threading.Tasks;
using Trident.Abstractions.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MetadataView : Page
{
    // Using a DependencyProperty as the backing store for VersionLoadingState.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VersionLoadingStateProperty =
        DependencyProperty.Register(nameof(VersionLoadingState), typeof(DataLoadingState), typeof(MetadataView),
            new PropertyMetadata(DataLoadingState.Idle));

    public MetadataView()
    {
        AttachmentView = new AdvancedCollectionView(ViewModel.Attachments) { Filter = Filter };
        InitializeComponent();
    }

    public MetadataViewModel ViewModel { get; } = App.ViewModel<MetadataViewModel>();

    public AdvancedCollectionView AttachmentView { get; }


    public DataLoadingState VersionLoadingState
    {
        get => (DataLoadingState)GetValue(VersionLoadingStateProperty);
        set => SetValue(VersionLoadingStateProperty, value);
    }


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ViewModel.OnAttached(e.Parameter);
        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.OnDetached();
        base.OnNavigatedFrom(e);
    }

    private async void AddLayerButton_OnClick(object sender, RoutedEventArgs e)
    {
        InputDialog dialog = new(XamlRoot) { Message = "Summarize usage of your new layer", Placeholder = "New Layer" };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.AddLayer(dialog.Result);
        }
    }

    private void AttachmentQueryBox_OnQuerySubmitted(AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args) =>
        AttachmentView.Refresh();

    private bool Filter(object obj)
    {
        if (obj is AttachmentModel attachment)
        {
            var query = AttachmentQueryBox.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            return attachment.ProjectName.Value.Contains(query, StringComparison.CurrentCultureIgnoreCase);
        }

        return false;
    }

    private void AddLoader(string identity)
    {
        VersionLoadingState = DataLoadingState.Loading;
        Task.Run(async () =>
        {
            var versions = await ViewModel.GetLoaderVersionsAsync(identity);

            DispatcherQueue.TryEnqueue(async () =>
            {
                VersionLoadingState = DataLoadingState.Idle;
                var dialog = new AddLoaderDialog(XamlRoot, identity, versions);
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    ViewModel.SelectedLayer?.Loaders.Add(new LoaderModel(new Loader(identity, dialog.SelectedVersion),
                        ViewModel.SelectedLayer.RemoveLoaderCommand));
                }
            });
        });
    }

    private void AddForgeButton_Click(object sender, RoutedEventArgs e) => AddLoader(Loader.COMPONENT_FORGE);

    private void AddNeoForgeButton_Click(object sender, RoutedEventArgs e) => AddLoader(Loader.COMPONENT_NEOFORGE);

    private void AddFabricButton_Click(object sender, RoutedEventArgs e) => AddLoader(Loader.COMPONENT_FABRIC);

    private void AddQuiltButton_Click(object sender, RoutedEventArgs e) => AddLoader(Loader.COMPONENT_QUILT);
}