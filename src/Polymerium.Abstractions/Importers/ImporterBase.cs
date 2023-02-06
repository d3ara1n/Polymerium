using System.IO;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.Importers;

public abstract class ImporterBase
{
    public abstract Task<Result<GameInstance, string>> ProcessAsync(Stream stream);

    public Result<GameInstance, string> Failed(string message)
    {
        return Result<GameInstance, string>.Err(message);
    }

    public Result<GameInstance, string> Finished(GameInstance instance)
    {
        return Result<GameInstance, string>.Ok(instance);
    }
}