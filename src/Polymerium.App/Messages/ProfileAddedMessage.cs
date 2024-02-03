using Trident.Abstractions.Profiles;

namespace Polymerium.App.Messages;

public class ProfileAddedMessage(string key, Profile item)
{
    public string Key { get; init; } = key;
    public Profile Item { get; init; } = item;
}