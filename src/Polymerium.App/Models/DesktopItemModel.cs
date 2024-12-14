using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Models;

public partial class DesktopItemModel(string key, string name, string version, string? loader, string? source)
    : ModelBase
{
    #region Reactive Properties

    [ObservableProperty] private string _name = name;

    #endregion

    public string Key => key;

    public static DesktopItemModel From(string key, Profile profile)
    {
        return new DesktopItemModel(key, profile.Name, profile.Setup.Version, profile.Setup.Loader,
            profile.Setup.Source);
    }

    public void Update(Profile profile)
    {
        Name = profile.Name;
    }
}