using Humanizer;
using PackageUrl;
using System;
using System.Linq;
using System.Windows.Input;
using Trident.Abstractions;
using static Trident.Abstractions.Metadata.Layer;
using static Trident.Abstractions.Profile.RecordData.TimelinePoint;

namespace Polymerium.App.Models
{
    public record EntryModel(string Key, Profile Inner, ICommand GotoDetailCommand)
    {
        public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? "/Assets/Placeholders/default_dirt.png";
        public string Version => Inner.Metadata.Version;
        public string Category => ExtractCategory();
        public DateTimeOffset? LastPlayAtRaw => ExtractDateTimeRaw(TimelimeAction.Play);
        public string PlayedAt => ExtractDateTime(TimelimeAction.Play);
        public string CreatedAt => ExtractDateTime(TimelimeAction.Create);
        public string DeployAt => ExtractDateTime(TimelimeAction.Deploy);
        public string Type => ExtractType();

        private string ExtractCategory()
        {
            try
            {
                var pkg = new PackageURL(Inner.Reference);
                return pkg.Type;
            }
            catch (MalformedPackageUrlException)
            {
                return "custom";
            }
        }

        private DateTimeOffset? ExtractDateTimeRaw(TimelimeAction action)
        {
            return Inner.Records.Timeline
                .Where(x => x.Action == action)
                .OrderByDescending(x => x.EndTime)
                .FirstOrDefault()?.EndTime;
        }

        private string ExtractDateTime(TimelimeAction action)
        {
            var record = ExtractDateTimeRaw(action);
            if (record != null)
                return record.Humanize();
            return "Never";
        }

        private string ExtractType()
        {
            var modloader = Inner.Metadata.Layers.SelectMany(x => x.Loaders).FirstOrDefault(x => Loader.MODLOADER_NAME_MAPPINGS.Keys.Contains(x.Id));
            if (modloader != null)
                return Loader.MODLOADER_NAME_MAPPINGS[modloader.Id];
            else
                return "Vanilla";
        }
    }
}
