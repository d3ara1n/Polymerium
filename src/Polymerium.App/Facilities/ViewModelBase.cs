using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia;

namespace Polymerium.App.Facilities;

public abstract class ViewModelBase : ObservableObject, IPageModel
{
    #region IPageModel Members

    public Task InitializeAsync(Dispatcher dispatcher, CancellationToken token) => OnInitializedAsync(dispatcher, token);

    public Task CleanupAsync(CancellationToken token) => OnCleanupAsync(token);

    #endregion

    protected virtual Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token) =>
        // Virtual function does nothing
        Task.CompletedTask;

    protected virtual Task OnCleanupAsync(CancellationToken token) =>
        // Virtual function does nothing
        Task.CompletedTask;
}