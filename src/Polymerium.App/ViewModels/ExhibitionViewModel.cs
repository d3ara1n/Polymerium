using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Polymerium.App.Facilities;

namespace Polymerium.App.ViewModels;

public class ExhibitionViewModel(ViewBag bag) : ViewModelBase
{
    public string Title { get; } = $"Exhibition({bag.Parameter ?? "None"})";

    protected override Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        return Task.Delay(TimeSpan.FromSeconds(5), token);
    }
}