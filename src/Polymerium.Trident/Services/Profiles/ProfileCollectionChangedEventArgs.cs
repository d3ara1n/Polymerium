using Trident.Abstractions;

namespace Polymerium.Trident.Services.Profiles;

public class ProfileCollectionChangedEventArgs(ProfileCollectionChangedAction action, string key, Profile item)
    : EventArgs
{
    public ProfileCollectionChangedAction Action { get; init; } = action;
    public string Key { get; init; } = key;
    public Profile Item { get; init; } = item;
}