using Humanizer;
using Humanizer.Localisation;
using Polymerium.App.Extensions;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Services;
using System;
using Trident.Abstractions;

namespace Polymerium.App.Models;

public record ProfileModel(string Key, Profile Inner, string? ThumbnailPath, InstanceStatusModel Status)
{
    public static readonly ProfileModel DUMMY = new(ProfileManager.DUMMY_KEY, ProfileManager.DUMMY_PROFILE, null,
        InstanceStatusModel.DUMMY);

    public string Thumbnail => ThumbnailPath ?? AssetPath.PLACEHOLDER_DEFAULT_DIRT;

    public string Type => Inner.ExtractTypeDisplay();

    public string Category => Inner.Reference?.Label ?? "custom";

    public DateTimeOffset? PlayedAtRaw =>
        Inner.Records.ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Play);

    public string PlayedAt => PlayedAtRaw?.Humanize() ?? "Never";

    public TimeSpan PlayTimeRaw =>
        Inner.Records.ExtractTimeSpan(Profile.RecordData.TimelinePoint.TimelimeAction.Play);

    public string PlayTime => PlayTimeRaw.Humanize(2, false, null, TimeUnit.Hour, TimeUnit.Second);

    public string Note
    {
        get => Inner.Records.Note;
        set => Inner.Records.Note = value;
    }

    public ReactiveCollection<Profile.RecordData.Todo, TodoModel> Todos { get; } =
        new(Inner.Records.Todos, x => new TodoModel(x), x => x.Inner);
}