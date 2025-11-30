using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
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

    public static readonly DirectProperty<InstancePackageDependencyModal, bool> IsAutoVersionProperty =
        AvaloniaProperty.RegisterDirect<InstancePackageDependencyModal, bool>(nameof(IsAutoVersion),
                                                                              o => o.IsAutoVersion,
                                                                              (o, v) => o.IsAutoVersion = v);

    public InstancePackageDependencyModal() => InitializeComponent();

    public required ProfileGuard Guard { get; init; }
    public required DataService DataService { get; init; }
    public required Filter Filter { get; init; }
    public required PersistenceService PersistenceService { get; init; }
    public required SourceCache<InstancePackageModel, Profile.Rice.Entry> Collection { get; init; }
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

    public bool IsAutoVersion
    {
        get;
        set => SetAndRaise(IsAutoVersionProperty, ref field, value);
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
                                                                              .Select(LoaderHelper.ToDisplayName)),
                                                                 string.Join(",", x.Requirements.AnyOfVersions),
                                                                 x.PublishedAt,
                                                                 x.ReleaseType,
                                                                 x.Dependencies))
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
                                                  versions.FirstOrDefault(x => x is InstancePackageVersionModel) as
                                                      InstancePackageVersionModel;
                                          });
                                      }
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
        var existing = Collection.Items.FirstOrDefault(x => x.Info?.Label == Model.Label
                                                         && x.Info?.Namespace == Model.Namespace
                                                         && x.Info?.ProjectId == Model.ProjectId);
        if (existing is not null)
        {
            // 已安装，直接关闭
            Dismiss();
            return;
        }

        // 创建新的 Entry 并添加到 Profile
        // 如果是自动版本，则不指定版本号（传 null）
        var versionId = IsAutoVersion ? null : SelectedVersion?.Id;
        var purl = PackageHelper.ToPurl(Model.Label, Model.Namespace, Model.ProjectId, versionId);
        var entry = new Profile.Rice.Entry(purl, true, null, []);

        // 创建新的 InstancePackageModel 并通知回调
        var newPackage = new InstancePackageModel(entry, false);
        OnPackageInstalledCallback(newPackage);

        Guard.Value.Setup.Packages.Add(entry);
        Collection.AddOrUpdate(newPackage);
        Guard.NotifyChanged();

        // 记录操作
        PersistenceService.AppendAction(new(Guard.Key, PersistenceService.ActionKind.EditPackage, null, purl));

        // NOTE: 这里有个非常别扭的地方就是如果想要被视为本地包，那么这个 InstancePackageModel.Info 必须已被赋值
        //  而这个 Info 只能是在 InstanceSetupView 中对新增的包引用进行刷新获取信息之后才会赋值
        //  想要通知 InstanceSetupView 触发刷新则需要 Guard.Dispose
        //  而原则上只有 InstancePackageModal 才会触发 Guard.Dispose
        //  有两个选择：
        //      - 提前用 Guard.Dispose 触发刷新，但是会导致 Guard 生命周期乱套，要是以后修改了 Guard 不允许二次处置就会爆炸
        //      - 在此处完成刷新，但是会导致刷新逻辑代码出现在两个地方，降低日后的可维护性
        //  这里通过修改 ProfileGuard 添加 NotifyChanged 来解决，即使用方案一
        // NOTE: 刚实践了一下发现更严重的问题，此处创建的 InstancePackageModel 和 InstanceSetupView 刷新中产生的不是同一个实例
        //  如果想要通过 OnPackageInstalledCallback 来搜索刷新中产生的 InstancePackageModel
        //  会遇到异步时序问题，刷新是后台异步的，没法等待，总不能让 InstancePackageModal 去监听刷新完成事件再赋值吧
        //  可以，但是那也太弯弯绕绕了
        //  于是这里使用了第三种方法，[将这里创建的 InstancePackageModel 提前加入到 Collection 里]，再通知刷新
        //  这样就会被判定为 ToUpdate 而不是 ToAdd，就能重复利用同一个 InstancePackageModel
        // NOTE: "将这里创建的 InstancePackageModel 提前加入到 Collection 里" 打破了刷新机制中的 Collection 前后一致性
        //  即 Collection 再外部被修改会导致一致性校验失效，真烦啊
        //  解决方案是降低一致性校验的等级，只要保证 Collection.Count == profile.Setup.Packages.Count + toAdd - toRemove 即可


        Dismiss();
    }

    #endregion
}
