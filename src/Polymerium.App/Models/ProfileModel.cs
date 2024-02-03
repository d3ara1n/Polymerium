using System;
using System.Windows.Input;
using Humanizer;
using Polymerium.App.Extensions;
using Trident.Abstractions.Profiles;

namespace Polymerium.App.Models;

public record ProfileModel(string Key, Profile Inner, ICommand DeleteTodoCommand)
{
    public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? AssetPath.PLACEHOLDER_DEFAULT_DIRT;

    public string Type => Inner.ExtractTypeDisplay();

    public DateTimeOffset? PlayedAtRaw =>
        Inner.ExtractDateTime(TimelimeAction.Play);

    public string PlayedAt => PlayedAtRaw?.Humanize() ?? "Never";

    public TimeSpan PlayTimeRaw => Inner.ExtractTimeSpan(TimelimeAction.Play);

    public string PlayTime => PlayTimeRaw.Humanize();

    public string Note
    {
        get => Inner.Records.Note;
        set => Inner.Records.Note = value;
    }

    public ReactiveCollection<Todo, TodoModel> Todos { get; } =
        new(Inner.Records.Todos, x => new TodoModel(x, DeleteTodoCommand), x => x.Inner);
}