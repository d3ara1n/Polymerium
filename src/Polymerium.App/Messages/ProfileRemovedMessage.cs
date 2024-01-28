using Trident.Abstractions;

namespace Polymerium.App.Messages;

public class ProfileRemovedMessage(string key, Profile item)
{
    public string Key { get; init; } = key;
    public Profile Item { get; init; } = item;
}