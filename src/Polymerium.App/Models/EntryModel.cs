using System;
using System.Windows.Input;
using Humanizer;
using PackageUrl;
using Polymerium.App.Extensions;
using Polymerium.Trident.Extensions;
using Trident.Abstractions;

namespace Polymerium.App.Models;

public record EntryModel(
    string Key,
    Profile Inner,
    string? ThumbnailPath,
    InstanceStatusModel Status,
    ICommand LaunchCommand,
    ICommand GotoInstanceViewCommand)
{
    public string Thumbnail => ThumbnailPath ?? "/Assets/Placeholders/default_dirt.png";
    public string Version => Inner.Metadata.Version;
    public string Category => ExtractCategory();

    public DateTimeOffset? LastPlayAtRaw =>
        Inner.Records.ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Play);

    public string PlayedAt => ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Play);
    public string CreatedAt => ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Create);
    public string DeployAt => ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Deploy);
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

    private string ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction action)
    {
        var record = Inner.Records.ExtractDateTime(action);
        return record != null ? record.Humanize() : "Never";
    }
}