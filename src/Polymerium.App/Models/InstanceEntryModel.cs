using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Models;

public partial class InstanceEntryModel : ModelBase
{
    public InstanceBasicModel Basic { get; }

    public InstanceEntryModel(string key, string name, string version, string? loader, string? source)
    {
        Basic = new InstanceBasicModel(key, name, version, loader, source);
    }
}