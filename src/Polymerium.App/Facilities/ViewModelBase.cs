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

    public virtual Task InitializeAsync(CancellationToken cancellationToken)
    {
        // 判断是否是 IStatedViewModel<T> 并注入 IStatedViewModel<T>.ViewState
        var statedInterface = GetType()
                             .GetInterfaces()
                             .FirstOrDefault(x => x.IsGenericType
                                               && x.GetGenericTypeDefinition() == typeof(IStatedViewModel<>));
        if (statedInterface is not null)
        {
            var stateType = statedInterface.GetGenericArguments().First();
            var viewStateService = Program.Services!.GetRequiredService<ViewStateService>();
            var state = viewStateService.RetrieveForView(this, stateType);
            statedInterface.GetProperty(nameof(IStatedViewModel<>.ViewState))!.SetValue(this, state);
        }

        return OnInitializeAsync(cancellationToken);
    }

    public virtual Task DeinitializeAsync()
    {
        // 判断是否是 IStatedViewModel<T> 并尝试释放
        var statedInterface = GetType()
                             .GetInterfaces()
                             .FirstOrDefault(x => x.IsGenericType
                                               && x.GetGenericTypeDefinition() == typeof(IStatedViewModel<>));
        if (statedInterface is not null)
        {
            var viewStateService = Program.Services!.GetRequiredService<ViewStateService>();
            viewStateService.ReleaseForView(this);
        }

        return OnDeinitializeAsync();
    }

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
