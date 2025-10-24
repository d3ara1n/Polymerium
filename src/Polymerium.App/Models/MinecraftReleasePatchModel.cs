using System;
using System.Collections.Generic;
using Humanizer;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class MinecraftReleasePatchModel(
    Uri cover,
    string category,
    string title,
    string description,
    DateTimeOffset publishedAt) : ModelBase
{
    #region Direct

    public string Title => title;
    public string Description => description;
    public Uri Cover => cover;
    public string Category => category;
    public DateTimeOffset PublishedAtRaw => publishedAt;
    public string PublishedAt => publishedAt.Humanize();

    #endregion
}
