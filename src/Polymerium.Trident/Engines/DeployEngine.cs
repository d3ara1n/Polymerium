using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Trident.Engines.Deploying;
using Polymerium.Trident.Engines.Deploying.Stages;
using Trident.Abstractions.FileModels;

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

public class DeployEngine(string key, Profile.Rice setup, IServiceProvider provider) : IAsyncEnumerable<StageBase>
{
    public IAsyncEnumerator<StageBase> GetAsyncEnumerator(CancellationToken token) =>
        new DeployEngineEnumerator(key, setup, provider, token);

    private class DeployEngineEnumerator(
        string key,
        Profile.Rice setup,
        IServiceProvider provider,
        CancellationToken token) : IAsyncEnumerator<StageBase>
    {
        private readonly DeployContext _context = new(key, setup, provider);

        private StageBase? DecideNext()
        {
            return null;
        }

        private T CreateStage<T>() where T : StageBase
        {
            var stage = ActivatorUtilities.CreateInstance<T>(_context.Provider);
            stage.Context = _context;
            return stage;
        }

        public void Reset() => throw new NotImplementedException();

        public async ValueTask<bool> MoveNextAsync()
        {
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

        public async ValueTask DisposeAsync()
        {
            // TODO 在此释放托管资源
        }
    }
}