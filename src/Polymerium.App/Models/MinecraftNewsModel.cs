using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class MinecraftNewsModel(
    Bitmap cover,
    string category,
    string title,
    string description,
    Uri readMoreLink,
    IReadOnlyList<string> tags) : ModelBase
{
    #region Direct

    public string Title => title;
    public string Description => description;
    public Bitmap Cover => cover;
    public string Category => category;
    public Uri ReadMoreLink => readMoreLink;
    public IReadOnlyList<string> Tags => tags;

    #endregion
}