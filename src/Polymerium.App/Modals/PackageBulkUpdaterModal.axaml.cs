﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services.Profiles;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Modals;

public partial class PackageBulkUpdaterModal : Modal
{
    public static readonly DirectProperty<PackageBulkUpdaterModal, IList<PackageUpdaterModel>> UpdatesProperty =
        AvaloniaProperty.RegisterDirect<PackageBulkUpdaterModal, IList<PackageUpdaterModel>>(nameof(Updates),
            o => o.Updates,
            (o, v) => o.Updates = v);

    private readonly CancellationTokenSource _cts = new();

    private ProfileGuard? _guard;

    public PackageBulkUpdaterModal()
    {
        InitializeComponent();
    }

    public IList<PackageUpdaterModel> Updates
    {
        get;
        set => SetAndRaise(UpdatesProperty, ref field, value);
    } = [];


    public required DataService DataService { get; init; }
    public required NotificationService NotificationService { get; init; }
    public required PersistenceService PersistenceService { get; init; }

    public void SetGuard(ProfileGuard guard, IList<PackageUpdaterModel> updates)
    {
        _guard = guard;
        Updates = updates;
    }

    #region Commands

    [RelayCommand]
    private void Dismiss()
    {
        RaiseEvent(new OverlayItem.DismissRequestedEventArgs(this));
    }

    [RelayCommand]
    private void Update()
    {
        foreach (var model in Updates.Where(x => x.IsChecked))
        {
            var old = model.Entry.Entry.Purl;
            model.Entry.Version = new InstancePackageVersionModel(model.NewVersionId,
                                                                  model.NewVersionName,
                                                                  string.Join(",",
                                                                              model.Package.Requirements.AnyOfLoaders
                                                                                 .Select(LoaderHelper.ToDisplayName)),
                                                                  string.Join(",",
                                                                              model.Package.Requirements.AnyOfVersions),
                                                                  model.NewVersionTimeRaw,
                                                                  model.Package.ReleaseType,
                                                                  model.Package.Dependencies);
            // 设置 Version 会同步到 Entry.Purl
            PersistenceService.AppendAction(new PersistenceService.Action(_guard!.Key,
                                                                          PersistenceService.ActionKind.EditPackage,
                                                                          old,
                                                                          model.Entry.Entry.Purl));
        }

        Dismiss();
    }

    #endregion
}