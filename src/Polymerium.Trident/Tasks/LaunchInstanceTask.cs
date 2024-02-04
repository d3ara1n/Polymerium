using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Tasks;

public class LaunchInstanceTask(string key) : TaskBase(key, nameof(LaunchInstanceTask), "...")
{
    protected override Task OnThreadAsync()
    {
        throw new NotImplementedException();
    }
}