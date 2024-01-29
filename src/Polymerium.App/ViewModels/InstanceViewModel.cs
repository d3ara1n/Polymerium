using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly ProfileManager _profileManager;

    private ProfileModel model = new(ProfileManager.DUMMY_KEY, ProfileManager.DUMMY_PROFILE);

    public InstanceViewModel(ProfileManager profileManager, NavigationService navigation)
    {
        _profileManager = profileManager;
        _navigation = navigation;

        GotoWorkbenchViewCommand = new RelayCommand<string>(GotoWorkbenchView);
    }

    public ProfileModel Model
    {
        get => model;
        set => SetProperty(ref model, value);
    }

    public ICommand GotoWorkbenchViewCommand { get; }

    public override bool OnAttached(object? maybeKey)
    {
        if (maybeKey is string key)
        {
            var profile = _profileManager.GetProfile(key);
            if (profile != null)
                Model = new ProfileModel(key, profile);
            return profile != null;
        }

        return false;
    }

    public override void OnDetached()
    {
        if (Model.Key != ProfileManager.DUMMY_KEY) _profileManager.Flush(Model.Key);
    }

    private void GotoWorkbenchView(string? key)
    {
        if (key != null && key != ProfileManager.DUMMY_KEY)
            _navigation.Navigate(typeof(WorkbenchView), key, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
    }
}