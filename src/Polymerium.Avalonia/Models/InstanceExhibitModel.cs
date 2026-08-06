using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using TridentCore.Abstractions.FileModels;

namespace Polymerium.Avalonia.Models;

// NOTE: Entry 是 instance 侧私有货物，与 Collect 落盘共享同一对象图；explorer 与 recipe 结构上不可见
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
