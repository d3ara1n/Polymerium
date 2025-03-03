using Avalonia.Media.Imaging;
using Polymerium.App.Facilities;
using System;
using System.Collections.Generic;

namespace Polymerium.App.Models;

public class MinecraftNewsModel(
    Uri cover,
    string category,
    string title,
    string description,
    Uri readMoreLink,
    IReadOnlyList<string> tags) : ModelBase
{
    public bool IsVeryBig { get; set; }

    #region Direct

    public string Title => title;
    public string Description => description;
    public Uri Cover => cover;
    public string Category => category;
    public Uri ReadMoreLink => readMoreLink;
    public IReadOnlyList<string> Tags => tags;

    #endregion
}