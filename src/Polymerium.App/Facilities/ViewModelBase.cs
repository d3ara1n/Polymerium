using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia;

namespace Polymerium.App.Facilities;

public abstract class ViewModelBase : ObservableObject, IPageModel
{
    #region IPageModel Members

    public Task InitializeAsync() => OnInitializeAsync();

    public Task DeinitializeAsync() => OnDeinitializeAsync();

    #endregion

    /// <summary>
    ///     该函数跑在 UI 线程 上，如果需要后台需要 Task.Run
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    protected virtual Task OnInitializeAsync() =>
        // Virtual function does nothing
        Task.CompletedTask;

    protected virtual Task OnDeinitializeAsync() =>
        // Virtual function does nothing
        Task.CompletedTask;

    public CancellationToken PageToken { get; set; } = CancellationToken.None;
}
