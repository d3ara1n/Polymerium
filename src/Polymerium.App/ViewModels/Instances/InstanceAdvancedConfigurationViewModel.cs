using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.Services;
using Polymerium.Core;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceAdvancedConfigurationViewModel : ObservableObject
{
    private readonly DispatcherQueue _dispatcher;
    private readonly IFileBaseService _fileBase;
    private readonly InstanceManager _instanceManager;
    private readonly ILogger _logger;
    private readonly NavigationService _navigation;

    public InstanceAdvancedConfigurationViewModel(ViewModelContext context, InstanceManager instanceManager,
        IFileBaseService fileBase, NavigationService navigation, ILogger<InstanceAdvancedConfigurationViewModel> logger)
    {
        Context = context;
        _instanceManager = instanceManager;
        _fileBase = fileBase;
        _navigation = navigation;
        _logger = logger;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        OpenRenameDialogCommand = new RelayCommand(OpenRenameDialog);
        DeleteInstanceCommand = new AsyncRelayCommand(DeleteInstanceAsync);
    }

    public ViewModelContext Context { get; }

    public ICommand OpenRenameDialogCommand { get; }
    public ICommand DeleteInstanceCommand { get; }
    
    public void OpenRenameDialog()
    {
        _dispatcher.TryEnqueue(async () =>
        {
            var instance = Context.AssociatedInstance;
            var dialog = new TextInputDialog();
            dialog.Title = "重命名";
            dialog.InputTextPlaceholder = instance.Name;
            dialog.Description = "这会同样会作用于实例所在目录";
            dialog.XamlRoot = App.Current.Window.Content.XamlRoot;
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                _instanceManager.RenameInstanceSafe(instance.Inner, dialog.InputText);
                Context.AssociatedInstance = instance;
            }
        });
    }
    
    public async Task DeleteInstanceAsync()
    {
        var dialog = new ConfirmationDialog
        {
            XamlRoot = App.Current.Window.Content.XamlRoot,
            Title = "Really?",
            Text = "此操作不可撤销"
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            var dir = new Uri($"poly-file://{Context.AssociatedInstance.Id}/");
            var path = _fileBase.Locate(dir);
            if (Directory.Exists(path))
                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Deleting instance {} local files failed", dir.AbsoluteUri);
                }

            _instanceManager.RemoveInstance(Context.AssociatedInstance.Inner);
        }
    }
}