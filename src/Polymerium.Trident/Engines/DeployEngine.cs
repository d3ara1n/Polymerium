using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Engines.Deploying.Stages;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Reactive;

namespace Polymerium.Trident.Engines;

// 构建过程
// 1.加载现有版本锁信息，验证是否可用（与 Setup 是否能完全对的上）
// 2.生成启用的包列表，解析出依赖图，扁平化
// 3.解析加载器，添加资源到版本锁
// 4.构建版本锁，并写入文件
// 5.构建固化清单
// 6.固化
// ...
// 版本锁中需要保存验证信息，例如当时的所有包列表

public class DeployEngine(
    string key,
    Profile.Rice setup,
    IServiceProvider provider,
    DeployEngineOptions options,
    string verificationWatermark) : IEnumerable<StageBase>
{
    #region IEnumerable<StageBase> Members

    public IEnumerator<StageBase> GetEnumerator() =>
        new DeployEngineEnumerator(new DeployContext(key, setup, provider, options, verificationWatermark));

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Nested type: DeployEngineEnumerator

    private class DeployEngineEnumerator(DeployContext context) : IEnumerator<StageBase>
    {
        #region IEnumerator<StageBase> Members

        public void Reset() => throw new NotImplementedException();

        public bool MoveNext()
        {
            if (Current is IDisposableLifetime disposable)
                disposable.Dispose();
            var next = DecideNext();
            if (next != null)
            {
                Current = next;
                return true;
            }

            Current = null!;
            return false;
        }

        public StageBase Current { get; private set; } = null!;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            // 中断导致没有 MoveNext
            if (Current is IDisposableLifetime disposable)
                disposable.Dispose();
        }

        #endregion

        private StageBase? DecideNext()
        {
            if (context.Manifest != null)
            {
                if (!context.IsSolidified)
                    return CreateStage<SolidifyManifestStage>();

                // Done
                return null;
            }

            if (context.Artifact != null)
            {
                if (context.Options.FastMode)
                    // Fast-Forward
                    return null;
                return CreateStage<GenerateManifestStage>();
            }


            if (context.ArtifactBuilder != null)
            {
                if (!context.IsVanillaInstalled)
                    return CreateStage<InstallVanillaStage>();

                if (!context.IsLoaderProcess)
                    return CreateStage<ProcessLoaderStage>();

                if (!context.IsPackageResolved)
                    return CreateStage<ResolvePackageStage>();

                return CreateStage<BuildArtifactStage>();
            }

            return CreateStage<CheckArtifactStage>();
        }

        private T CreateStage<T>() where T : StageBase
        {
            var stage = ActivatorUtilities.CreateInstance<T>(context.Provider);
            stage.Context = context;
            return stage;
        }
    }

    #endregion
}