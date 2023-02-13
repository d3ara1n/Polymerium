using CommunityToolkit.Mvvm.Input;

namespace Polymerium.App.Models;

public class InstanceComponentItemModel
{
    public InstanceComponentItemModel(
        string id,
        string thumbnailSource,
        string friendlyName,
        string version,
        IRelayCommand<InstanceComponentItemModel> removeCommand
    )
    {
        Id = id;
        ThumbnailSource = thumbnailSource;
        FriendlyName = friendlyName;
        Version = version;
        RemoveCommand = removeCommand;
    }

    public string Id { get; set; }
    public string ThumbnailSource { get; set; }
    public string FriendlyName { get; set; }
    public string Version { get; set; }

    public IRelayCommand<InstanceComponentItemModel> RemoveCommand { get; set; }
}
