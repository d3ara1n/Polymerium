using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Utilities;
using Trident.Core.Services.Profiles;

namespace Polymerium.App.Modals;

public partial class InstancePackageDependencyModal : Modal
{
    public static readonly DirectProperty<InstancePackageDependencyModal, LazyObject?> LazyVersionsProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageDependencyModal, LazyObject?>(nameof(LazyVersions),
            o => o.LazyVersions,
            (o, v) => o.LazyVersions = v);

    public static readonly DirectProperty<InstancePackageDependencyModal, bool> IsFilterEnabledProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageDependencyModal, bool>(nameof(IsFilterEnabled),
                                                                              o => o.IsFilterEnabled,
                                                                              (o, v) => o.IsFilterEnabled = v);

    public static readonly DirectProperty<InstancePackageDependencyModal, InstancePackageVersionModel?>
        SelectedVersionProperty =
            AvaloniaProperty
               .RegisterDirect<InstancePackageDependencyModal, InstancePackageVersionModel?>(nameof(SelectedVersion),
                    o => o.SelectedVersion,
                    (o, v) => o.SelectedVersion = v);

    public InstancePackageDependencyModal() => InitializeComponent();

    public required ProfileGuard Guard { get; init; }
    public required DataService DataService { get; init; }
    public required Filter Filter { get; init; }
    public required PersistenceService PersistenceService { get; init; }
    public required IReadOnlyList<InstancePackageModel> Collection { get; init; }
    public required Action<InstancePackageModel> OnPackageInstalledCallback { get; init; }

    private InstancePackageDependencyModel Model => (InstancePackageDependencyModel)DataContext!;

    public LazyObject? LazyVersions
    {
        get;
        set => SetAndRaise(LazyVersionsProperty, ref field, value);
    }

    public bool IsFilterEnabled
    {
        get;
        set => SetAndRaise(IsFilterEnabledProperty, ref field, value);
    }

    public InstancePackageVersionModel? SelectedVersion
    {
        get;
        set => SetAndRaise(SelectedVersionProperty, ref field, value);
    }

    private LazyObject ConstructVersions()
    {
        var lazy = new LazyObject(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var versions = await DataService.InspectVersionsAsync(Model.Label,
                                                                  Model.Namespace,
                                                                  Model.ProjectId,
                                                                  IsFilterEnabled ? Filter : Filter.None);
            return new InstancePackageVersionCollection([
                .. versions.Select(x => new InstancePackageVersionModel(x.VersionId,
                                                                        x.VersionName,
                                                                        string.Join(",",
                                                                            x.Requirements.AnyOfLoaders
                                                                             .Select(LoaderHelper
                                                                                 .ToDisplayName)),
                                                                        string.Join(",", x.Requirements.AnyOfVersions),
                                                                        x.PublishedAt,
                                                                        x.ReleaseType,
                                                                        x.Dependencies))
            ]);
        });
        return lazy;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        IsFilterEnabled = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsFilterEnabledProperty)
        {
            LazyVersions = ConstructVersions();
        }
    }

    #region Commands

    private bool CanInstall() => SelectedVersion is not null;

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void Install()
    {
        if (SelectedVersion is null)
        {
            return;
        }

        // 检查是否已安装
        var existing = Collection.FirstOrDefault(x => x.Info?.Label == Model.Label
                                                   && x.Info?.Namespace == Model.Namespace
                                                   && x.Info?.ProjectId == Model.ProjectId);
        if (existing is not null)
        {
            // 已安装，直接关闭
            Dismiss();
            return;
        }

        // 创建新的 Entry 并添加到 Profile
        var purl = PackageHelper.ToPurl(Model.Label, Model.Namespace, Model.ProjectId, SelectedVersion.Id);
        var entry = new Profile.Rice.Entry(purl, true, null, []);
        Guard.Value.Setup.Packages.Add(entry);

        // 记录操作
        PersistenceService.AppendAction(new(Guard.Key, PersistenceService.ActionKind.EditPackage, null, purl));

        // 创建新的 InstancePackageModel 并通知回调
        var newPackage = new InstancePackageModel(entry, false);
        OnPackageInstalledCallback(newPackage);

        Dismiss();
    }

    #endregion
}
