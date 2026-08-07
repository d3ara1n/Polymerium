using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using SkiaSharp;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using TridentCore.Pref;

namespace Polymerium.Avalonia.PageModels;

public partial class InstanceActivitiesPageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    InstanceStateAggregator aggregator,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    DataService dataService,
    PersistenceService persistenceService) : InstancePageModelBase(context, aggregator, instanceManager, profileManager)
{
    private const int ActionPageSize = 20;

    #region Other

    private SKColor GetAccentColorFromResources()
    {
        try
        {
            if (Application.Current?.TryGetResource("ControlAccentInteractiveBackgroundBrush", null, out var resource)
             == true
             && resource is SolidColorBrush brush)
            {
                var avaloniaColor = brush.Color;
                return new(avaloniaColor.R, avaloniaColor.G, avaloniaColor.B, avaloniaColor.A);
            }
        }
        catch { }

        return SKColors.DodgerBlue;
    }

    private void LoadActionPage(int pageIndex)
    {
        var lazy = new LazyObject(async _ =>
        {
            var actions = persistenceService.GetActions(Basic.Key, pageIndex, ActionPageSize, out var totalCount);
            ActionTotalCount = totalCount;

            // 过滤掉 Old/New 全空的无效记录（无任何包引用，展示无意义）
            var valid = actions.Where(x => !(x.Old == null && x.New == null)).ToList();

            // 收集页面内所有 Pref，解析为 identifier 后去重，一次性批量解析。
            // 批量解析会把失败项单独放进 Failed，不会因为单条失败连累整页。
            var prefToId = new Dictionary<string, PackageIdentifier>();
            foreach (var pref in valid
                                .SelectMany(x => new[] { x.Old, x.New })
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Select(s => s!)
                                .Distinct())
            {
                if (PackageHelper.TryParse(pref, out var id))
                {
                    prefToId[pref] = id;
                }
            }

            var batch = await dataService.ResolvePackagesAsync(prefToId.Values.Distinct(), Filter.None);

            Package? ResolveByPref(string? pref) =>
                pref is not null
             && prefToId.TryGetValue(pref, out var id)
             && batch.Successful.TryGetValue(id, out var pkg)
                    ? pkg
                    : null;

            // 预取缩略图（每个 Uri 一次），单张失败不连累整页
            var thumbnails = new Dictionary<Uri, Bitmap>();
            var thumbnailUris = valid
                               .Select(x => ResolveByPref(x.New) ?? ResolveByPref(x.Old))
                               .Where(p => p?.Thumbnail != null)
                               .Select(p => p!.Thumbnail!)
                               .Distinct()
                               .ToList();

            if (thumbnailUris.Count > 0)
            {
                await Task.WhenAll(thumbnailUris.Select(async uri =>
                {
                    try
                    {
                        thumbnails[uri] = await dataService.GetBitmapAsync(uri);
                    }
                    catch
                    {
                        // 单张缩略图获取失败不阻塞整页，退化为默认图
                    }
                }));
            }

            var results = valid
                         .Select(x =>
                          {
                              var newPkg = ResolveByPref(x.New);
                              var oldPkg = ResolveByPref(x.Old);
                              var primary = newPkg ?? oldPkg;

                              var model = new InstanceActionModel(x.Old,
                                                                  x.New,
                                                                  DateTimeHelper.FromPersistedLocalDateTime(x.At),
                                                                  false);

                              if (primary != null)
                              {
                                  model.IsLoaded = true;
                                  var thumbUri = newPkg?.Thumbnail ?? oldPkg?.Thumbnail;
                                  var thumbnail = thumbUri is not null && thumbnails.TryGetValue(thumbUri, out var bmp)
                                                      ? bmp
                                                      : AssetUriIndex.DirtImageBitmap;
                                  model.Info = new(primary.ProjectName,
                                                   oldPkg?.VersionName,
                                                   newPkg?.VersionName,
                                                   thumbnail);
                              }

                              return model;
                          })
                         .ToList();

            return new InstanceActionCollection(results);
        });
        PagedActions = lazy;
    }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        TotalPlayTimeRaw = persistenceService.GetTotalPlayTime(Basic.Key);

        ActionPageIndex = 0;
        LoadActionPage(0);

        int[] days = [-6, -5, -4, -3, -2, -1, 0];
        var times = days
                   .Select(x => persistenceService.GetDayPlayTime(Basic.Key, DateTime.Now.AddDays(x)))
                   .Select(x => x.TotalHours)
                   .ToArray();

        var accentColor = GetAccentColorFromResources();

        WeekSeries =
        [
            new ColumnSeries<double>(times) { Name = "Play Time (Hours)", Fill = new SolidColorPaint(accentColor) }
        ];

        var dayLabels = days
                       .Select(x => DateTimeOffset.Now.AddDays(x).DayOfWeek switch
                        {
                            DayOfWeek.Sunday => "Sun",
                            DayOfWeek.Monday => "Mon",
                            DayOfWeek.Tuesday => "Tue",
                            DayOfWeek.Wednesday => "Wed",
                            DayOfWeek.Thursday => "Thu",
                            DayOfWeek.Friday => "Fri",
                            DayOfWeek.Saturday => "Sat",
                            _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
                        })
                       .ToArray();
        XAxes = [new() { Labels = dayLabels, ForceStepToMin = true, MinStep = 1 }];

        YAxes = [new() { Name = "Hours", MinLimit = 0, Labeler = value => $"{value:F1}h" }];

        TotalPlayTimeRank = persistenceService.GetTotalPlayTimeRank(Basic.Key);
        SessionCount = persistenceService.GetSessionCount(Basic.Key);
        ActiveDays = persistenceService.GetActiveDays(Basic.Key);
        CrashCount = persistenceService.GetCrashCount(Basic.Key);

        var lastActivity = persistenceService.GetLastActivity(Basic.Key);
        LastPlayedAt = lastActivity?.End;
        var firstActivity = persistenceService.GetFirstActivity(Basic.Key);
        FirstPlayedAt = firstActivity?.Begin;
        LongestSessionRaw = persistenceService.GetLongestSession(Basic.Key);

        PlaytimePercentage = persistenceService.GetPercentageInTotalPlayTime(Basic.Key) * 100.0;
        ThisWeekPlayTimeRaw = persistenceService.GetWeekPlayTime(Basic.Key, 0);
        LastWeekPlayTimeRaw = persistenceService.GetWeekPlayTime(Basic.Key, 1);

        return Task.CompletedTask;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(ActionPageIndex))
        {
            LoadActionPage(ActionPageIndex);
        }
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial LazyObject? PagedActions { get; set; }

    public double TotalHours => TotalPlayTimeRaw.TotalHours;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalHours))]
    public partial TimeSpan TotalPlayTimeRaw { get; set; }

    [ObservableProperty]
    public partial int TotalPlayTimeRank { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SuccessRate))]
    public partial int SessionCount { get; set; }

    [ObservableProperty]
    public partial int ActiveDays { get; set; }

    [ObservableProperty]
    public partial int ActionPageIndex { get; set; }

    [ObservableProperty]
    public partial int ActionTotalCount { get; set; }

    [ObservableProperty]
    public partial ISeries<double>[]? WeekSeries { get; set; }

    [ObservableProperty]
    public partial IEnumerable<Axis>? XAxes { get; set; }

    [ObservableProperty]
    public partial IEnumerable<Axis>? YAxes { get; set; }

    // 健康度相关属性
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SuccessRate))]
    public partial int CrashCount { get; set; }

    // 正常运行率（计算属性）
    public double SuccessRate => SessionCount > 0 ? (double)(SessionCount - CrashCount) / SessionCount * 100 : 100.0;

    // Statistics Tab 属性
    // 最后一次游戏时间
    [ObservableProperty]
    public partial DateTime? LastPlayedAt { get; set; }

    // 首次游戏时间
    [ObservableProperty]
    public partial DateTime? FirstPlayedAt { get; set; }

    // 最长单次游戏时长
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LongestSessionHours))]
    public partial TimeSpan LongestSessionRaw { get; set; }

    public double LongestSessionHours => LongestSessionRaw.TotalHours;

    // 平均每次游戏时长（计算属性）
    public double AverageSessionMinutes => SessionCount > 0 ? TotalPlayTimeRaw.TotalMinutes / SessionCount : 0;

    // Trends Tab 属性
    // 占总游戏时间百分比
    [ObservableProperty]
    public partial double PlaytimePercentage { get; set; }

    // 本周游戏时间
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThisWeekPlayTimeHours))]
    public partial TimeSpan ThisWeekPlayTimeRaw { get; set; }

    public double ThisWeekPlayTimeHours => ThisWeekPlayTimeRaw.TotalHours;

    // 上周游戏时间
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastWeekPlayTimeHours))]
    public partial TimeSpan LastWeekPlayTimeRaw { get; set; }

    public double LastWeekPlayTimeHours => LastWeekPlayTimeRaw.TotalHours;

    // 周对比变化率（计算属性）
    public double WeekChangePercentage =>
        LastWeekPlayTimeRaw.TotalHours > 0
            ?
            (ThisWeekPlayTimeRaw.TotalHours - LastWeekPlayTimeRaw.TotalHours) / LastWeekPlayTimeRaw.TotalHours * 100
            : ThisWeekPlayTimeRaw.TotalHours > 0
                ? 100
                : 0;

    #endregion
}
