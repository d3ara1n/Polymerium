using System;
using System.Windows.Input;

namespace Polymerium.App.Models;

public class RecentPlayedItemModel
{
    public string InstanceId { get; set; }
    public string ThumbnailFile { get; set; }
    public string Name { get; set; }
    public DateTimeOffset LastPlayedAt { get; set; }
    public ICommand Command { get; set; }
}