using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels.Instances;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceMetadataConfigurationView : Page
{
    public InstanceMetadataConfigurationView()
    {
        ViewModel =
            App.Current.Provider.GetRequiredService<InstanceMetadataConfigurationViewModel>();
        InitializeComponent();
    }

    public InstanceMetadataConfigurationViewModel ViewModel { get; }

    private void AttachmentBox_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.IsAttachmentBeingParsed = true;
        Task.Run(() => ViewModel.LoadParseAttachmentsAsync(model => DispatcherQueue.TryEnqueue(() =>
        {
            if (model != null)
                ViewModel.Attachments.Add(model);
            else
                ViewModel.IsAttachmentBeingParsed = false;
        })));
    }
}