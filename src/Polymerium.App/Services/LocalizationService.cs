using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;

namespace Polymerium.App.Services
{
    public class LocalizationService
    {
        private readonly IResourceManager _resourceManager;
        private readonly ResourceContext _resourceContext;
        private readonly ResourceMap _languageManifest;


        public LocalizationService(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            _resourceContext = resourceManager.CreateResourceContext();
            _languageManifest = resourceManager.MainResourceMap.GetSubtree("LanguageManifest");
        }

        public IEnumerable<(string, string)> GetSupportedLanguages()
        {
            var count = _languageManifest.ResourceCount;
            for(uint i = 0; i < count ; i++)
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
    }
}
