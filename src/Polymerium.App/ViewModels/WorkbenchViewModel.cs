using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.Trident.Services;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels;

public class WorkbenchViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcher;
    private readonly ProfileManager _profileManager;
    private readonly RepositoryService _repositoryService;

    private WorkpieceModel model;
    public WorkpieceModel Model
    {
        get => model;
        set => SetProperty(ref model, value);
    }

    public WorkbenchViewModel(RepositoryService repositoryService, ProfileManager profileManager)
    {
        _profileManager = profileManager;
        _repositoryService = repositoryService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        model = new(_repositoryService, _dispatcher, ProfileManager.DUMMY_KEY, ProfileManager.DUMMY_PROFILE);
    }

    public override bool OnAttached(object? maybeKey)
    {
        if (maybeKey is string key)
        {
            var profile = _profileManager.GetProfile(key);
            if (profile != null)
                Model = new WorkpieceModel(_repositoryService, _dispatcher, key, profile);
            return profile != null;
        }

        return false;
    }

    public override void OnDetached()
    {
        if (Model.Key != ProfileManager.DUMMY_KEY) _profileManager.Flush(Model.Key);
    }
}