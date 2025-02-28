using System.Collections;
using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class ResolvePackageStage(RepositoryAgent agent) : StageBase
{
    protected override async Task OnProcessAsync(CancellationToken token)
    {
        var builder = Context.ArtifactBuilder!;
        var purls = new Stack<Purl>(Context
                                   .Setup.Stage.Concat(Context.Setup.Stash)
                                   .Select(x =>
                                    {
                                        if (PackageHelper.TryParse(x, out var parsed))
                                        {
                                            return new Purl(new Identity(parsed.Label, parsed.Namespace, parsed.Pid),
                                                            parsed.Vid);
                                        }

                                        throw new FormatException($"Package {x} is not a valid package");
                                    }));
        var flatten = new Dictionary<Identity, Version>();

        // 不同于依赖解决方案，由于各个资源平台本身就没考虑过版本兼容性，这里直接按可用的最高版本选择
        while (purls.TryPop(out var parsed))
        {
            var resolved = await agent.ResolveAsync(parsed.Identity.Label,
                                                    parsed.Identity.Namespace,
                                                    parsed.Identity.Pid,
                                                    parsed.Vid,
                                                    Filter.Empty with
                                                    {
                                                        Loader = Context.Setup.Loader,
                                                        Version = Context.Setup.Version
                                                    });
            var version = new Version(resolved.Kind,
                                      resolved.PublishedAt,
                                      resolved.FileName,
                                      resolved.Sha1,
                                      resolved.Download);

            if (flatten.TryGetValue(parsed.Identity, out var old))
            {
                if (old.ReleasedAt < resolved.PublishedAt)
                {
                    flatten[parsed.Identity] = version;
                    // 该版本对应的依赖图也应该替换掉，但这里不管，忽略掉
                }
            }
            else
            {
                flatten.Add(parsed.Identity, version);
                foreach (var dep in resolved.Dependencies)
                {
                    purls.Push(new Purl(new Identity(dep.Label, dep.Namespace, dep.Pid), dep.Vid));
                }
            }
        }

        foreach (var (key, value) in flatten)
        {
            // TODO
        }
    }

    private record Purl(Identity Identity, string? Vid);

    private record Identity(string Label, string? Namespace, string Pid);

    private record Version(ResourceKind Kind, DateTimeOffset ReleasedAt, string FileName, string? Sha1, Uri Download);
}