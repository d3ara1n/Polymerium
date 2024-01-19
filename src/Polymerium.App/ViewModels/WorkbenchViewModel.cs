using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public class WorkbenchViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcher;
    private readonly ProfileManager _profileManager;
    private readonly RepositoryAgent repositoryAgent;

    private WorkpieceModel model;

    public WorkbenchViewModel(RepositoryAgent repositoryAgent, ProfileManager profileManager)
    {
        _profileManager = profileManager;
        this.repositoryAgent = repositoryAgent;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        FetchAttachmentCommand = new RelayCommand<AttachmentModel>(FetchAttachment);

        model = new WorkpieceModel(this.repositoryAgent, _dispatcher, ProfileManager.DUMMY_KEY,
            ProfileManager.DUMMY_PROFILE, FetchAttachmentCommand);
    }

    public WorkpieceModel Model
    {
        get => model;
        set => SetProperty(ref model, value);
    }

    public ICommand FetchAttachmentCommand { get; }

    public override bool OnAttached(object? maybeKey)
    {
        if (maybeKey is string key)
        {
            var profile = _profileManager.GetProfile(key);
            if (profile != null)
                Model = new WorkpieceModel(repositoryAgent, _dispatcher, key, profile, FetchAttachmentCommand);
            return profile != null;
        }

        return false;
    }

    public override void OnDetached()
    {
        if (Model.Key != ProfileManager.DUMMY_KEY) _profileManager.Flush(Model.Key);
    }

    private void FetchAttachment(AttachmentModel? model)
    {
        if (model != null) model.Fetch();
    }
}