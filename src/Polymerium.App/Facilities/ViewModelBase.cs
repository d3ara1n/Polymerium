using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia;

namespace Polymerium.App.Facilities
{
    public abstract class ViewModelBase : ObservableObject, IPageModel
    {
        #region IPageModel Members

        public Task InitializeAsync(CancellationToken token) => OnInitializeAsync(token);

        public Task DeinitializeAsync(CancellationToken token) => OnDeinitializeAsync(token);

        #endregion

        /// <summary>
        ///     该函数跑在 UI 线程 上，如果需要后台需要 Task.Run
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected virtual Task OnInitializeAsync(CancellationToken token) =>
            // Virtual function does nothing
            Task.CompletedTask;

        protected virtual Task OnDeinitializeAsync(CancellationToken token) =>
            // Virtual function does nothing
            Task.CompletedTask;
    }
}
