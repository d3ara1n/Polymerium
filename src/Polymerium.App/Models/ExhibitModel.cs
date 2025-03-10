﻿using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class ExhibitModel : ModelBase
{
    public ExhibitModel(
        string label,
        string? ns,
        string pid,
        string name,
        string summary,
        Uri thumbnail,
        string author,
        IReadOnlyList<string> tags,
        DateTimeOffset updatedAt,
        ulong downloads,
        Uri reference)
    {
        _label = label;
        _ns = ns;
        _pid = pid;
        _name = name;
        _summary = summary;
        _thumbnail = thumbnail;
        _author = author;
        _tags = tags;
        _updatedAt = updatedAt.Humanize();
        _downloads = ((double)downloads).ToMetric(decimals: 2);
        _reference = reference;
    }

    #region Reactive

    [ObservableProperty]
    private string _label;

    [ObservableProperty]
    private string? _ns;

    [ObservableProperty]
    private string _pid;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _summary;

    [ObservableProperty]
    private Uri _thumbnail;

    [ObservableProperty]
    private string _author;

    [ObservableProperty]
    private IReadOnlyList<string> _tags;

    [ObservableProperty]
    private string _updatedAt;

    [ObservableProperty]
    private string _downloads;

    [ObservableProperty]
    private Uri _reference;

    #endregion
}