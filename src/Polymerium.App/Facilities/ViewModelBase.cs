using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia;

namespace Polymerium.App.Facilities;

public abstract class ViewModelBase : ObservableObject, IPageModel
{
    #region IPageModel Members

    public Task InitializeAsync(CancellationToken token) => OnInitializedAsync(token);

    public Task CleanupAsync(CancellationToken token) => OnDeinitializeAsync(token);

    #endregion

    protected virtual Task OnInitializedAsync(CancellationToken token) =>
        // Virtual function does nothing
        Task.CompletedTask;

    protected virtual Task OnDeinitializeAsync(CancellationToken token) =>
        // Virtual function does nothing
        Task.CompletedTask;
}