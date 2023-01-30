using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Abstractions;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ObservableObject
{
    private readonly InstanceManager _instanceManager;
    private readonly IOverlayService _overlayService;

    private GameInstance instance;

    public InstanceViewModel(InstanceManager instanceManager, IOverlayService overlayService)
    {
        _instanceManager = instanceManager;
        _overlayService = overlayService;
    }

    public GameInstance Instance
    {
        get => instance;
        set => SetProperty(ref instance, value);
    }

    public void GotInstance(GameInstance instance)
    {
        Instance = instance;
    }

    [RelayCommand]
    public void Start()
    {
        var dialog = new PrepareGameDialog(Instance, _overlayService);
        _overlayService.Show(dialog);
    }
}