using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia;

namespace Polymerium.App.Facilities;

public class ViewModelBase : ObservableObject, IPageModel
{
    public Task InitializeAsync(Dispatcher dispatcher, CancellationToken token)
    {
        return OnInitializedAsync(dispatcher, token);
    }

    public Task CleanupAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    protected virtual Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        // Virtual function does nothing
        return Task.CompletedTask;
    }
}