using CommunityToolkit.Mvvm.Messaging;
using Polymerium.App.Messages;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Profiles;

namespace Polymerium.App.Services;

public class MessageService
{
    public MessageService(ProfileManager profileManager)
    {
        profileManager.ProfileCollectionChanged += ProfileManager_ProfileCollectionChanged;
    }

    private void ProfileManager_ProfileCollectionChanged(ProfileManager sender, ProfileCollectionChangedEventArgs args)
    {
        switch (args.Action)
        {
            case ProfileCollectionChangedAction.Add:
                WeakReferenceMessenger.Default.Send(new ProfileAddedMessage(args.Key, args.Item));
                break;
            case ProfileCollectionChangedAction.Remove:
                WeakReferenceMessenger.Default.Send(new ProfileRemovedMessage(args.Key, args.Item));
                break;
        }
    }
}