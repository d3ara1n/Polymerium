using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Polymerium.App.Facilities;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel(ViewBag bag) : ViewModelBase
{
    protected override async Task OnInitializedAsync(Dispatcher dispatcher, CancellationToken token)
    {
        if (bag is { Parameter: int number })
        {
        }
    }
}