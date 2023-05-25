using System.Threading;
using System.Threading.Tasks;
using Polymerium.Abstractions.Meta;
using Polymerium.Core.Components.Installers;

namespace Polymerium.Core.Components;

public abstract class ComponentInstallerBase
{
    // 输出 libraries，参数, main class overrides, crate(按等级会向前覆盖)
    public ComponentInstallerContext Context { get; set; } = null!;
    public CancellationToken Token { get; set; } = CancellationToken.None;

    public abstract Task<ComponentInstallerError?> StartAsync(Component component);

    public ComponentInstallerError? Finished()
    {
        return null;
    }

    public ComponentInstallerError? Failed(ComponentInstallerError reason)
    {
        return reason;
    }

    public ComponentInstallerError? Canceled()
    {
        return ComponentInstallerError.Canceled;
    }
}
