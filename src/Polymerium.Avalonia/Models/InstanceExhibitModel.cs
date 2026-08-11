using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using TridentCore.Abstractions.FileModels;

namespace Polymerium.Avalonia.Models;

public partial class InstanceExhibitModel(
    string label,
    string? @namespace,
    string projectId,
    string projectName,
    string summary,
    Uri thumbnail,
    string author,
    IReadOnlyList<string> tags,
    DateTimeOffset updatedAt,
    ulong downloads,
    Uri reference) : ExhibitModel(label,
                                  @namespace,
                                  projectId,
                                  projectName,
                                  summary,
                                  thumbnail,
                                  author,
                                  tags,
                                  updatedAt,
                                  downloads,
                                  reference)
{
    [ObservableProperty]
    public partial Profile.Rice.Entry? Entry { get; set; }
}
