using Polymerium.App.Extensions;

namespace Polymerium.App.Models;

// 用于从 InstanceManager 中跟踪状态并反馈到界面上
public record InstanceStatusModel
{
    private InstanceState state = InstanceState.Idle;

    public InstanceStatusModel(string key)
    {
        Key = key;

        State = this.ToBindable(x => x.state, (x, v) => x.state = v);
    }

    public string Key { get; }

    public Bindable<InstanceStatusModel, InstanceState> State { get; }

    public void OnStateChanged(InstanceState changed)
    {
        State.Value = changed;
    }
}