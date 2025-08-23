using Polymerium.Trident.Exceptions;
using Polymerium.Trident.Services;
using Polymerium.Trident.Utilities;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public class EnsureRuntimeStage(PrismLauncherService prismLauncherService) : StageBase
    {
        protected override async Task OnProcessAsync(CancellationToken token)
        {
            var major = Context.Artifact!.JavaMajorVersion;

            var bad = false;

            try
            {
                _ = Context.JavaHomeLocator(major);
            }
            catch (JavaNotFoundException)
            {
                bad = true;
            }

            if (bad)
            {
                var manifest = await prismLauncherService.GetRuntimeAsync(major, token).ConfigureAwait(false);
                var osString = $"{PlatformHelper.GetOsName()}-{PlatformHelper.GetOsArch()}";
                var runtime = manifest
                             .Runtimes.OrderBy(x => x.ReleaseTime)
                             .FirstOrDefault(x => x.RuntimeOS == osString);
                if (runtime != default)
                {
                    Context.Runtime = new(major, runtime.Vendor, runtime.Url, true);
                }

                // 如果获取不到也什么都不做，悄咪咪把错误留给 Launch Flow 再爆出来
            }

            Context.IsRuntimeEnsured = true;
        }
    }
}
