using Polymerium.Abstractions;

namespace Polymerium.Core;

public class GameManagedInstance
{
    public GameManagedInstance(GameInstance instance)
    {
    }

    public GameInstance Inner { get; set; }
}