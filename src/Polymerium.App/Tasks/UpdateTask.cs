using Trident.Abstractions;

namespace Polymerium.App.Tasks;

public class UpdateTask : TaskBase
{
    public UpdateTask(string key, Metadata metadata) : base(key, $"Updating {key}...", "Preparing")
    {
    }
}