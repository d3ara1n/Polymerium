using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services.Profiles;

namespace Polymerium.Avalonia.Modals;

public partial class InstancePackageDependencyModal : Modal
{
    public static readonly DirectProperty<
        InstancePackageDependencyModal,
        LazyObject?
    > LazyVersionsProperty = AvaloniaProperty.RegisterDirect<
        InstancePackageDependencyModal,
        LazyObject?
    >(nameof(LazyVersions), o => o.LazyVersions, (o, v) => o.LazyVersions = v);

    public static readonly DirectProperty<
        InstancePackageDependencyModal,
        bool
    > IsFilterEnabledProperty = AvaloniaProperty.RegisterDirect<
        InstancePackageDependencyModal,
        bool
    >(nameof(IsFilterEnabled), o => o.IsFilterEnabled, (o, v) => o.IsFilterEnabled = v);

    public static readonly DirectProperty<
        InstancePackageDependencyModal,
        InstancePackageVersionModel?
    > SelectedVersionProperty = AvaloniaProperty.RegisterDirect<
        InstancePackageDependencyModal,
        InstancePackageVersionModel?
    >(nameof(SelectedVersion), o => o.SelectedVersion, (o, v) => o.SelectedVersion = v);

    public static readonly DirectProperty<
        InstancePackageDependencyModal,
        bool
    > IsAutoVersionProperty = AvaloniaProperty.RegisterDirect<InstancePackageDependencyModal, bool>(
        nameof(IsAutoVersion),
        o => o.IsAutoVersion,
        (o, v) => o.IsAutoVersion = v
    );

    public InstancePackageDependencyModal() => InitializeComponent();

    public required ProfileGuard Guard { get; init; }
    public required DataService DataService { get; init; }
    public required Filter Filter { get; init; }
    public required PersistenceService PersistenceService { get; init; }
    public required SourceCache<PackageListItemBase, PackageListKey> Collection { get; init; }

    private IEnumerable<InstancePackageModel> Packages =>
        Collection.Items.OfType<PackageListItemBase.Entry>().Select(i => i.Package);

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

    public bool IsAutoVersion
    {
        get;
        set => SetAndRaise(IsAutoVersionProperty, ref field, value);
    }

    private LazyObject ConstructVersions()
    {
        var lazy = new LazyObject(
            async t =>
            {
                if (t.IsCancellationRequested)
                {
                    return null;
                }

                var versions = await DataService.InspectVersionsAsync(
                    Model.Label,
                    Model.Namespace,
                    Model.ProjectId,
                    IsFilterEnabled ? Filter : Filter.None
                );
                return new InstancePackageVersionCollection([
                    .. versions.Select(x => new InstancePackageVersionModel(
                        x.VersionId,
                        x.VersionName,
                        string.Join(
                            ",",
                            x.Requirements.AnyOfLoaders.Select(LoaderHelper.ToDisplayName)
                        ),
                        string.Join(",", x.Requirements.AnyOfVersions),
                        x.PublishedAt,
                        x.ReleaseType,
                        x.Dependencies
                    )),
                ]);
            },
            value =>
            {
                // Auto-select the first (best) version after loading
                if (value is InstancePackageVersionCollection { Count: > 0 } versions)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        SelectedVersion =
                            versions.FirstOrDefault(x => x is InstancePackageVersionModel)
                            as InstancePackageVersionModel;
                    });
                }
            }
        );
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

        if (change.Property == IsAutoVersionProperty || change.Property == SelectedVersionProperty)
        {
            InstallCommand.NotifyCanExecuteChanged();
        }
    }

    #region Commands

    private bool CanInstall() => IsAutoVersion || SelectedVersion is not null;

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task Install()
    {
        // 检查是否已安装
        var existing = Packages.FirstOrDefault(x =>
            x.Info?.Label == Model.Label
            && x.Info?.Namespace == Model.Namespace
            && x.Info?.ProjectId == Model.ProjectId
        );
        if (existing is not null)
        {
            // 已安装，直接关闭
            Dismiss();
            return;
        }

        // 创建新的 Entry 并添加到 Profile
        // 如果是自动版本，则不指定版本号（传 null）
        var versionId = IsAutoVersion ? null : SelectedVersion?.Id;
        var pref = PackageHelper.ToPref(Model.Label, Model.Namespace, Model.ProjectId, versionId);
        var entry = new Profile.Rice.Entry()
        {
            Pref = pref,
            Enabled = true,
            Source = null,
        };

        // 加入 Profile，由 InstanceSetupPage 的 merge 造 Entry item 与实例并加载 Info
        Guard.Value.Setup.Packages.Add(entry);
        Guard.NotifyChanged();

        // 记录操作
        PersistenceService.AppendAction(
            new()
            {
                Key = Guard.Key,
                Kind = PersistenceService.ActionKind.EditPackage,
                Old = null,
                New = pref,
            }
        );

        Dismiss();
    }

    #endregion
}
