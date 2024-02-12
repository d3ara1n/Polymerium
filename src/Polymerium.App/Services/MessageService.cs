using CommunityToolkit.Mvvm.Messaging;
using Polymerium.App.Messages;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Polymerium.Trident.Services.Profiles;

namespace Polymerium.App.Services;

public class MessageService
{
    public MessageService(ProfileManager profileManager, InstanceManager instanceManager)
    {
        profileManager.ProfileCollectionChanged += ProfileManager_ProfileCollectionChanged;

        instanceManager.InstanceLaunching += InstanceManagerOnInstanceLaunching;
        instanceManager.InstanceDeploying += InstanceManagerOnInstanceDeploying;
    }

    private void InstanceManagerOnInstanceDeploying(InstanceManager sender, InstanceDeployingEventArgs args)
    {
        WeakReferenceMessenger.Default.Send(new InstanceDeployingMessage(args.Key, args.Handle));
    }

    private void InstanceManagerOnInstanceLaunching(InstanceManager sender, InstanceLaunchingEventArgs args)
    {
        WeakReferenceMessenger.Default.Send(new InstanceLaunchingMessage(args.Key, args.Handle, args.Mode));
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