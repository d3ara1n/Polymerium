using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Widgets;
using TridentCore.Abstractions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.PageModels;

public abstract partial class InstancePageModelBase : ViewModelBase
{
    protected InstancePageModelBase(
        IViewContext<InstanceContextParameter> context,
        InstanceStateAggregator aggregator,
        InstanceManager instanceManager,
        ProfileManager profileManager)
    {
        _aggregator = aggregator;
        InstanceManager = instanceManager;
        ProfileManager = profileManager;
        if (context.Parameter is not null)
        {
            Basic = context.Parameter.Basic;
            Widgets = context.Parameter.Widgets;
        }
        else
        {
            throw new PageNotReachedException(GetType(), "Basic to the instance is not provided");
        }
    }

    #region Reactive

    [ObservableProperty]
    public partial InstanceState State { get; set; } = InstanceState.Idle;

    #endregion

    #region Nested type: InstanceContextParameter

    public record InstanceContextParameter(InstanceBasicModel Basic, WidgetBase[] Widgets);

    #endregion

    #region Protected

    protected virtual void OnModelUpdated(string key, Profile profile) { }

    /// <summary>一次新活动开始（按 <see cref="InstanceActivity.Id" /> 判定）。</summary>
    protected virtual void OnActivityStarted(InstanceActivity activity) { }

    /// <summary>同一活动的后继快照（进度/字段变化）。</summary>
    protected virtual void OnActivityProgressed(InstanceActivity activity) { }

    /// <summary>活动落终态。参数即终态快照，失败原因等信息均已带全。</summary>
    protected virtual void OnActivityCompleted(InstanceActivity activity) { }

    #endregion

    #region Injected Protected

    protected readonly InstanceManager InstanceManager;
    protected readonly ProfileManager ProfileManager;
    private readonly InstanceStateAggregator _aggregator;
    private IDisposable? _aggregatorSubscription;
    private Guid? _currentActivityId;

    #endregion

    #region Tracking

    public override Task InitializeAsync(CancellationToken cancellationToken)
    {
        ProfileManager.ProfileUpdated += OnProfileUpdated;

        _aggregatorSubscription = _aggregator
                                 .Watch(Basic.Key)
                                 .Subscribe(activity =>
                                  {
                                      // 活动对象是不可变值，投递到 UI 线程后仍然有效——无需担心它在
                                      // 投递期间被释放。
                                      if (activity is null)
                                      {
                                          _currentActivityId = null;
                                          Dispatcher.UIThread.Post(() => State = InstanceState.Idle);
                                          return;
                                      }

                                      var started = _currentActivityId != activity.Id;
                                      _currentActivityId = activity.IsCompleted ? null : activity.Id;

                                      Dispatcher.UIThread.Post(() =>
                                      {
                                          State = activity.IsCompleted ? InstanceState.Idle : activity.Kind;
                                          if (started)
                                          {
                                              OnActivityStarted(activity);
                                          }

                                          if (activity.IsCompleted)
                                          {
                                              OnActivityCompleted(activity);
                                          }
                                          else if (!started)
                                          {
                                              OnActivityProgressed(activity);
                                          }
                                      });
                                  });

        OnModelUpdated(Basic.Key, ProfileManager.GetImmutable(Basic.Key));

        return base.InitializeAsync(cancellationToken);
    }

    public override Task DeinitializeAsync()
    {
        ProfileManager.ProfileUpdated -= OnProfileUpdated;
        _aggregatorSubscription?.Dispose();
        return base.DeinitializeAsync();
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        if (e.Key != Basic.Key)
        {
            return;
        }

        OnModelUpdated(e.Key, e.Value);
    }

    #endregion

    #region Direct

    public InstanceBasicModel Basic { get; }

    public WidgetBase[] Widgets { get; }

    #endregion
}
