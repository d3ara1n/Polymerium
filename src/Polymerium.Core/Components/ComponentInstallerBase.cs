using System.Threading;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;

namespace Polymerium.Core.Components;

public abstract class ComponentInstallerBase
{
    // 输出 libraries，参数, main class overrides, crate(按等级会向前覆盖)
    public ComponentInstallerContext Context { get; set; }
    public CancellationToken Token { get; set; } = CancellationToken.None;

    public abstract Task<Result<string>> StartAsync(Component component);

    public Result<string> Finished()
    {
        return Result<string>.Ok();
    }

    public Result<string> Failed(string reason)
    {
        return Result<string>.Err(reason);
    }

    public Result<string> Canceled()
    {
        return Failed(string.Empty);
    }
}