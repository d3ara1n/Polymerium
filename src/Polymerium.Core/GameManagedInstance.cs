using Polymerium.Abstractions;

namespace Polymerium.Core;

public class GameManagedInstance
{
    public GameManagedInstance(GameInstance instance)
    {
        Inner = instance;
    }

    public GameInstance Inner { get; set; }
}