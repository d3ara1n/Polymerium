using System;
using Humanizer;
using Polymerium.App.Extensions;
using Polymerium.Trident.Extensions;
using Trident.Abstractions;

namespace Polymerium.App.Models;

public record ProfileModel(string Key, Profile Inner, string? ThumbnailPath)
{
    public string Thumbnail => ThumbnailPath ?? AssetPath.PLACEHOLDER_DEFAULT_DIRT;

    public string Type => Inner.ExtractTypeDisplay();

    public DateTimeOffset? PlayedAtRaw =>
        Inner.Records.ExtractDateTime(Profile.RecordData.TimelinePoint.TimelimeAction.Play);

    public string PlayedAt => PlayedAtRaw?.Humanize() ?? "Never";

    public TimeSpan PlayTimeRaw => Inner.Records.ExtractTimeSpan(Profile.RecordData.TimelinePoint.TimelimeAction.Play);

    public string PlayTime => PlayTimeRaw.Humanize();

    public string Note
    {
        get => Inner.Records.Note;
        set => Inner.Records.Note = value;
    }

    public ReactiveCollection<Profile.RecordData.Todo, TodoModel> Todos { get; } =
        new(Inner.Records.Todos, x => new TodoModel(x), x => x.Inner);
}