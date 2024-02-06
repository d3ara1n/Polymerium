using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.Trident.Services;

namespace Polymerium.App.ViewModels;

public class MetadataViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcher;
    private readonly ProfileManager _profileManager;
    private readonly RepositoryAgent repositoryAgent;

    private MetadataModel model = new(ProfileManager.DUMMY_KEY, ProfileManager.DUMMY_PROFILE);

    public MetadataViewModel(RepositoryAgent repositoryAgent, ProfileManager profileManager)
    {
        _profileManager = profileManager;
        this.repositoryAgent = repositoryAgent;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        FetchAttachmentCommand = new RelayCommand<AttachmentModel>(FetchAttachment);
    }

    public MetadataModel Model
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
                Model = new MetadataModel(key, profile);
            return profile != null;
        }

        return false;
    }

    public override void OnDetached()
    {
        if (Model.Key != ProfileManager.DUMMY_KEY) _profileManager.Flush(Model.Key);
    }

    private void FetchAttachment(AttachmentModel? item)
    {
        //if (model != null) model.Fetch();
    }
}