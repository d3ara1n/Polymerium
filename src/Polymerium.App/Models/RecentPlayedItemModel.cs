using System.Windows.Input;

namespace Polymerium.App.Models;

public class RecentPlayedItemModel
{
    public RecentPlayedItemModel(string instanceId, string? thumbnailFile, string name, string lastPlayedAt,
        ICommand command)
    {
        InstanceId = instanceId;
        ThumbnailFile = thumbnailFile;
        Name = name;
        LastPlayedAt = lastPlayedAt;
        Command = command;
    }

    public string InstanceId { get; set; }
    public string? ThumbnailFile { get; set; }
    public string Name { get; set; }
    public string LastPlayedAt { get; set; }
    public ICommand Command { get; set; }
}