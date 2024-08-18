using Trident.Abstractions;

namespace Polymerium.App.Messages;

public class ProfileRemovedMessage(string key, Profile item)
{
    public string Key => key;
    public Profile Item => item;
}