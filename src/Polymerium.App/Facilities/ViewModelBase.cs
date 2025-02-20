using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.Facilities;

public abstract class ViewModelBase : ObservableObject, IPageModel
{
    public Task InitializeAsync(Dispatcher dispatcher, CancellationToken token) =>
        OnInitializedAsync(dispatcher, token);

    public Task CleanupAsync(CancellationToken token) => OnCleanupAsync(token);

    protected virtual Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token) =>
        // Virtual function does nothing
        Task.CompletedTask;

    protected virtual Task OnCleanupAsync(CancellationToken token) =>
        // Virtual function does nothing
        Task.CompletedTask;
}