using System;
using System.Windows.Input;
using Humanizer;
using PackageUrl;
using Polymerium.App.Extensions;
using Trident.Abstractions;
using static Trident.Abstractions.Profile.RecordData.TimelinePoint;

namespace Polymerium.App.Models;

public record EntryModel(string Key, Profile Inner, ICommand GotoInstanceViewCommand)
{
    public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? "/Assets/Placeholders/default_dirt.png";
    public string Version => Inner.Metadata.Version;
    public string Category => ExtractCategory();
    public DateTimeOffset? LastPlayAtRaw => Inner.ExtractDateTime(TimelimeAction.Play);
    public string PlayedAt => ExtractDateTime(TimelimeAction.Play);
    public string CreatedAt => ExtractDateTime(TimelimeAction.Create);
    public string DeployAt => ExtractDateTime(TimelimeAction.Deploy);
    public string Type => Inner.ExtractTypeDisplay();

    private string ExtractCategory()
    {
        if (Inner.Reference != null)
            try
            {
                var pkg = new PackageURL(Inner.Reference);
                return pkg.Type;
            }
            catch (MalformedPackageUrlException)
            {
                return "invalid";
            }

        return "custom";
    }

    private string ExtractDateTime(TimelimeAction action)
    {
        var record = Inner.ExtractDateTime(action);
        return record != null ? record.Humanize() : "Never";
    }
}