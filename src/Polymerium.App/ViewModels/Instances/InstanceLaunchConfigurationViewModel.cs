using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Dialogs;
using Polymerium.App.Models;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceLaunchConfigurationViewModel : ObservableObject
{
    private readonly JavaManager _javaManager;

    public InstanceLaunchConfigurationViewModel(ViewModelContext context, JavaManager javaManager)
    {
        Instance = context.AssociatedInstance!;
        _javaManager = javaManager;
        Configuration = Instance.Configuration;
        OpenPickerAsyncCommand = new AsyncRelayCommand(OpenPickerAsync);
    }

    public ConfigurationModel Configuration { get; }

    public GameInstanceModel Instance { get; }

    public ICommand OpenPickerAsyncCommand { get; }

    public async Task OpenPickerAsync()
    {
        var dialog = new JavaPickerDialog
        {
            XamlRoot = App.Current.Window.Content.XamlRoot,
            JavaInstallations = _javaManager
                .QueryJavaInstallations()
                .Select(x =>
                {
                    var option = _javaManager.VerifyJavaHome(x);
                    if (option.TryUnwrap(out var model))
                        return model;
                    return null;
                })
                .Where(x => x != null)
                .Select(x => x!)
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            Configuration.JavaHome = dialog.SelectedJava.HomePath;
    }
}
