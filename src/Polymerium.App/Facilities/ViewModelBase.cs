using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Mvvm.Models;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Services;

namespace Polymerium.App.Facilities;

public abstract class ViewModelBase : ObservableObject, IViewModel
{
    #region IViewModel Members

    public virtual Task InitializeAsync(CancellationToken cancellationToken) =>
        OnInitializeAsync(cancellationToken);

    public virtual Task DeinitializeAsync() => OnDeinitializeAsync();

    #endregion

    /// <summary>
    ///     该函数跑在 UI 线程 上，如果需要后台需要 <see cref="Task.Run" />
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    protected virtual Task OnInitializeAsync(CancellationToken token) =>
        // Virtual function does nothing
        Task.CompletedTask;

    protected virtual Task OnDeinitializeAsync() =>
        // Virtual function does nothing
        Task.CompletedTask;
}
