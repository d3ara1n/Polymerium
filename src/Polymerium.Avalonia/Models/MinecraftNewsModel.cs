using System;
using Humanizer;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class MinecraftNewsModel(
    Uri cover,
    string category,
    string title,
    string description,
    Uri readMoreLink,
    DateOnly publishedAt
) : ModelBase
{
    #region Direct

    public string Title => title;
    public string Description => description;
    public Uri Cover => cover;
    public string Category => category;
    public Uri ReadMoreLink => readMoreLink;
    public DateOnly PublishedAtRaw => publishedAt;
    public string PublishedAt => publishedAt.ToDateTime(TimeOnly.MinValue).Humanize();

    #endregion
}
