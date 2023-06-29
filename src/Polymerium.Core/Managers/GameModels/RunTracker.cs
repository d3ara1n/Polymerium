using Polymerium.Abstractions;

namespace Polymerium.Core.Managers.GameModels
{
    public class RunTracker
    {
        public RunTracker(GameInstance instance)
        {
            Instance = instance;
        }

        public GameInstance Instance { get; set; }
    }
}
