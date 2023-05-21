using System.Collections.Generic;
using Windows.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace Polymerium.App.Services;

public class LocalizationService
{
    private readonly ResourceMap _languageManifest;
    private readonly ResourceContext _resourceContext;
    private readonly IResourceManager _resourceManager;


    public LocalizationService(IResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
        _resourceContext = resourceManager.CreateResourceContext();
        _languageManifest = resourceManager.MainResourceMap.GetSubtree("LanguageManifest");
    }

    public IEnumerable<(string, string)> GetSupportedLanguages()
    {
        var count = _languageManifest.ResourceCount;
        for (uint i = 0; i < count; i++)
        {
            var item = _languageManifest.GetValueByIndex(i, _resourceContext);
            yield return (item.Key, item.Value.ValueAsString);
        }
    }

    public string GetString(string key, string? fallback = null)
    {
        fallback ??= key;
        var candiate = _resourceManager.MainResourceMap.TryGetValue($"Resources/{key}", _resourceContext);
        return candiate?.ValueAsString ?? fallback;
    }

    public void SetLanguageByKey(string key)
    {
        ApplicationLanguages.PrimaryLanguageOverride = key;
        _resourceContext.QualifierValues["Language"] = key;
    }
}