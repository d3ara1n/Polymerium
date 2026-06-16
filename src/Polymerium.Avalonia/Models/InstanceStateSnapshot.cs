using TridentCore.Abstractions;
using TridentCore.Abstractions.Tasks;

namespace Polymerium.Avalonia.Models;

/// <summary>
///     实例状态的扁平快照，由 <see cref="InstanceStateAggregator" /> 合成。
///     消费方只需这一个类型，不再接触 <see cref="TrackerBase" /> 的异构进度流。
/// </summary>
/// <param name="State">实例当前活动类型（Installing/Updating/Deploying/Running）。</param>
/// <param name="Progress">归一化进度（None/Indeterminate/Determinate），UI 据此决定进度条可见性与填充。</param>
/// <param name="Tracker">
///     钻取引用：需要细粒度信息（如 Dashboard 取 LaunchTracker.Process、HomePage 取
///     DeployTracker.CurrentStage）的消费方可下转型访问。常规状态展示无需触碰。
/// </param>
public sealed record InstanceStateSnapshot(
    string Key,
    InstanceState State,
    TrackerProgress Progress,
    TrackerBase? Tracker);
