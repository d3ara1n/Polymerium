using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Polymerium.App.Facilities;

namespace Polymerium.App.ViewModels;

public class UnknownViewModel(ViewBag bag) : ViewModelBase
{
    public string Title { get; } = $"User's Unknown Playground({bag.Parameter ?? "None"})";

    protected override Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        return Task.Delay(TimeSpan.FromSeconds(5), token);
    }
}