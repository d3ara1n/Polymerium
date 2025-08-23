using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Polymerium.Trident.Services;
using Polymerium.Trident.Utilities;
using Trident.Abstractions;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.Trident.Engines.Deploying.Stages
{
    public class ResolvePackageStage(ILogger<ResolvePackageStage> logger, RepositoryAgent agent) : StageBase
    {
        public Subject<(int, int)> ProgressStream { get; } = new();

        protected override async Task OnProcessAsync(CancellationToken token)
        {
            var builder = Context.ArtifactBuilder!;

            string? loader = null;
            if (Context.Setup.Loader != null)
            {
                if (LoaderHelper.TryParse(Context.Setup.Loader, out var result))
                {
                    loader = result.Identity;
                }
                else
                {
                    throw new FormatException($"{Context.Setup.Loader} is not well formatted loader string");
                }
            }

            var purls = new ConcurrentStack<Purl>(Context
                                                 .Setup.Packages.Where(x => x.Enabled)
                                                 .Select(x =>
                                                  {
                                                      if (PackageHelper.TryParse(x.Purl, out var parsed))
                                                      {
                                                          return new Purl(new(parsed.Label,
                                                                              parsed.Namespace,
                                                                              parsed.Pid),
                                                                          parsed.Vid,
                                                                          false);
                                                      }

                                                      throw new
                                                          FormatException($"Package {x.Purl} is not a valid package");
                                                  }));
            var flatten = new ConcurrentDictionary<Identity, Version>();

            ProgressStream.OnNext((0, purls.Count));

            // 不同于依赖解决方案，由于各个资源平台本身就没考虑过版本兼容性，这里直接按可用的最高版本选择
            var tasks = Enumerable.Range(0, Math.Max(Environment.ProcessorCount / 2, 1)).Select(_ => ResolveAsync());

            await Task.WhenAll(tasks).ConfigureAwait(false);

            if (token.IsCancellationRequested)
            {
                return;
            }

            foreach (var (key, value) in flatten)
            {
                builder.AddParcel(key.Label,
                                  key.Namespace,
                                  key.Pid,
                                  value.Vid,
                                  Path.Combine(PathDef.Default.DirectoryOfBuild(Context.Key),
                                               FileHelper.GetAssetFolderName(value.Kind),
                                               value.FileName),
                                  value.Download,
                                  value.Sha1);
            }

            Context.IsPackageResolved = true;
            return;

            async Task ResolveAsync()
            {
                while (purls.TryPop(out var parsed) && !token.IsCancellationRequested)
                {
                    try
                    {
                        var resolved = await agent
                                            .ResolveAsync(parsed.Identity.Label,
                                                          parsed.Identity.Namespace,
                                                          parsed.Identity.Pid,
                                                          parsed.Vid,
                                                          Filter.None with
                                                          {
                                                              Loader = loader, Version = Context.Setup.Version
                                                          })
                                            .ConfigureAwait(false);
                        logger.LogDebug("Resolved {} package {}({}/{}) with {}",
                                        parsed.IsPhantom ? "phantom" : "non-phantom",
                                        resolved.ProjectName,
                                        resolved.ProjectId,
                                        resolved.VersionId,
                                        resolved.Dependencies.Any()
                                            ? $"[{string.Join(",", resolved.Dependencies)}]"
                                            : "no dependencies");
                        var version = new Version(resolved.VersionId,
                                                  resolved.Kind,
                                                  resolved.PublishedAt,
                                                  resolved.FileName,
                                                  resolved.Sha1,
                                                  resolved.Download,
                                                  parsed.Vid != null);

                        if (flatten.TryGetValue(parsed.Identity, out var old))
                        {
                            if (!old.IsReliable)
                            {
                                if (version.IsReliable || old.ReleasedAt < resolved.PublishedAt)
                                {
                                    flatten[parsed.Identity] = version;
                                }
                            }
                            // 该版本对应的依赖图也应该替换掉，但这里不管，忽略掉
                        }
                        else
                        {
                            flatten.TryAdd(parsed.Identity, version);
                            // NOTE: 实测有些模组的依赖是互相冲突的，这游戏的资源托管站数据就是依托狗屎
                            if (Context.Options.ResolveDependency)
                            {
                                foreach (var dep in resolved.Dependencies.Where(x => x.IsRequired))
                                {
                                    purls.Push(new(new(dep.Label, dep.Namespace, dep.Pid), dep.Vid, true));
                                }
                            }
                        }
                    }
                    catch
                    {
                        if (!parsed.IsPhantom)
                        {
                            throw;
                        }
                        else
                        {
                            logger.LogWarning("Phantom package {} has been referred incorrectly", parsed.Identity);
                        }
                    }
                    finally
                    {
                        ProgressStream.OnNext((flatten.Count, purls.Count + flatten.Count));
                    }
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            ProgressStream.Dispose();
        }

        #region Nested type: Identity

        private record Identity(string Label, string? Namespace, string Pid);

        #endregion

        #region Nested type: Purl

        private record Purl(Identity Identity, string? Vid, bool IsPhantom);

        #endregion

        #region Nested type: Version

        private record Version(
            string Vid,
            ResourceKind Kind,
            DateTimeOffset ReleasedAt,
            string FileName,
            string? Sha1,
            Uri Download,
            bool IsReliable = true);

        #endregion
    }
}
