namespace Polymerium.App.Models;

public class SettingLanguageItemModel
{
    public SettingLanguageItemModel(string key, string displayName)
    {
        Key = key;
        DisplayName = displayName;
    }

    public string Key { get; set; }
    public string DisplayName { get; set; }
}
