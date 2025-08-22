using System;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public class ExhibitDependencyModel(
    ExhibitModel exhibit,
    string label,
    string? @namespace,
    string pid,
    string? vid,
    string projectName,
    Uri thumbnail,
    string author,
    ResourceKind kind,
    bool isRequired) : ModelBase
{
    // 就算提供了 VID，也只需要显示 ProjectName，不关心版本

    #region Direct

    public ExhibitModel Exhibit => exhibit;
    public string Label => label;
    public string? Namespace => @namespace;
    public string Pid => pid;
    public string? Vid => vid;
    public string ProjectName => projectName;
    public Uri Thumbnail => thumbnail;
    public string Author => author;
    public ResourceKind Kind => kind;
    public bool IsRequired => isRequired;

    #endregion
}