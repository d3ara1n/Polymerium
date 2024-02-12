using System.Threading.Tasks;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Tasks;

public class DeployInstanceTask(string key, string profileName, DeployTracker tracker)
    : TaskBase(key, $"Launch {profileName}", "Preparing...")
{
    protected override async Task OnThreadAsync()
    {
    }
}