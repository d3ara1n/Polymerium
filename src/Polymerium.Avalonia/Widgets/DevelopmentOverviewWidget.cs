using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Markup.Xaml;
using Huskui.Avalonia.Models;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.Widgets;

public partial class DevelopmentOverviewWidget : WidgetBase
{
    private IDisposable? _changedSubscription;

    public DevelopmentOverviewWidget() => AvaloniaXamlLoader.Load(this);

    #region Nested type: Presentation

    // 状态一次取全，两份列表共用同一次取数结果：全量给详细形态，去掉无事可报的行给紧凑形态。
    public sealed record Presentation(IReadOnlyList<DevelopmentStatusModel> All,
                                      IReadOnlyList<DevelopmentStatusModel> Meaningful);

    #endregion

    #region Reactive

    public static readonly DirectProperty<DevelopmentOverviewWidget, LazyObject?> LazyStateProperty =
        AvaloniaProperty.RegisterDirect<DevelopmentOverviewWidget, LazyObject?>(nameof(LazyState),
                                                                               o => o.LazyState,
                                                                               (o, v) => o.LazyState = v);

    public LazyObject? LazyState
    {
        get;
        set => SetAndRaise(LazyStateProperty, ref field, value);
    }

    #endregion

    protected override Task OnInitializeAsync()
    {
        Title = LanguageManager.Instance.DevelopmentOverviewWidget_Title.Current();

        var service = Context.Provider.GetRequiredService<InstanceStateService>();
        var key = Context.Key;
        LazyState = ConstructState(service, key);

        // 实例状态被撤销（部署开始/结束、profile 变更）时换一个新的 LazyObject：LazyObject 对已完成值
        // 短路，必须换对象才会重新取数。
        _changedSubscription = service.Changed.Where(changed => changed == key)
                                     .Subscribe(_ => LazyState = ConstructState(service, key));

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        _changedSubscription?.Dispose();
        _changedSubscription = null;
        LazyState = null;

        return Task.CompletedTask;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LazyStateProperty && change.OldValue is LazyObject previous)
        {
            previous.Cancel();
        }
    }

    private static LazyObject ConstructState(InstanceStateService service, string key) =>
        new(async token =>
        {
            var gitTask = service.RetrieveGitAsync(key, token);
            var packSourceTask = service.RetrievePackSourceAsync(key, token);
            var changesTask = service.RetrieveChangesAsync(key, token);
            var patchesTask = service.RetrievePatchesAsync(key, token);
            var attachmentsTask = service.RetrieveAttachmentsAsync(key, token);
            var lastRunTask = service.RetrieveLastRunAsync(key, token);
            await Task.WhenAll(gitTask, packSourceTask, changesTask, patchesTask, attachmentsTask, lastRunTask);

            var git = await gitTask;
            var packSource = await packSourceTask;
            var changes = await changesTask;
            var patches = await patchesTask;
            var attachments = await attachmentsTask;
            var lastRun = await lastRunTask;

            var all = new List<DevelopmentStatusModel>
            {
                new DevelopmentStatusModel.Git
                {
                    IsRepository = git.IsRepository,
                    BranchName = git.BranchName,
                    HeadSummary = git.HeadSummary,
                    TrackingBranchName = git.TrackingBranchName,
                    AheadCount = git.AheadCount,
                    BehindCount = git.BehindCount,
                    ChangedCount = git.ChangedCount
                },
                new DevelopmentStatusModel.Unsynced { Count = changes.Entries.Count },
                new DevelopmentStatusModel.PackSource { FileCount = packSource.FileCount },
                new DevelopmentStatusModel.Patches
                {
                    ImportCount = patches.ImportCount,
                    UserCount = patches.UserCount
                },
                new DevelopmentStatusModel.ReleaseFiles
                {
                    IconPath = attachments.IconPath,
                    ReadmePath = attachments.ReadmePath,
                    ChangelogPath = attachments.ChangelogPath,
                    LicensePath = attachments.LicensePath
                },
                new DevelopmentStatusModel.LastRun
                {
                    EndedAt = lastRun.EndedAt,
                    Outcome = lastRun.Outcome,
                    CrashCount = lastRun.CrashCount
                }
            };

            return new Presentation(all, [.. all.Where(IsMeaningful)]);
        });

    // 无事可报的行不进紧凑形态：没有版本控制、没有待同步、没有 Pack Source、没有 Patch、
    // 发布文件齐全、还没跑过——都只说明「没什么要做的」。
    private static bool IsMeaningful(DevelopmentStatusModel status) => status switch
    {
        DevelopmentStatusModel.Git git => git.IsRepository,
        DevelopmentStatusModel.Unsynced unsynced => unsynced.Count > 0,
        DevelopmentStatusModel.PackSource packSource => packSource.FileCount > 0,
        DevelopmentStatusModel.Patches patches => patches.ImportCount > 0 || patches.UserCount > 0,
        DevelopmentStatusModel.ReleaseFiles releaseFiles => releaseFiles.IconPath is null
                                                         || releaseFiles.ReadmePath is null
                                                         || releaseFiles.ChangelogPath is null
                                                         || releaseFiles.LicensePath is null,
        DevelopmentStatusModel.LastRun lastRun => lastRun.EndedAt is not null || lastRun.CrashCount > 0,
        _ => false
    };
}
