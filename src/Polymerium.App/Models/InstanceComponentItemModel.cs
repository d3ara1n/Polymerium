using CommunityToolkit.Mvvm.Input;

namespace Polymerium.App.Models;

public class InstanceComponentItemModel
{
    public string Id { get; set; }
    public string ThumbnailSource { get; set; }
    public string FriendlyName { get; set; }
    public string Version { get; set; }

    public IRelayCommand<InstanceComponentItemModel> RemoveCommand { get; set; }
}