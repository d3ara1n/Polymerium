using System.Collections;
using Polymerium.Trident.Engines.Deploying.Stages;

namespace Polymerium.Trident.Engines;

// 构建过程
// 1.加载现有版本锁信息，验证是否可用（与 Setup 是否能完全对的上）
// 2.生成启用的包列表，解析出依赖图，扁平化
// ...
// 版本锁中需要保存验证信息，例如当时的所有包列表

public class DeployEngine : IEnumerable<StageBase>
{
    public IEnumerator<StageBase> GetEnumerator() => new DeployEngineEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public class DeployEngineEnumerator() : IEnumerator<StageBase>
    {
        private StageBase? _current;
        public bool MoveNext() => throw new NotImplementedException();

        public void Reset() => throw new NotImplementedException();

        StageBase IEnumerator<StageBase>.Current => _current ?? throw new InvalidOperationException();

        object? IEnumerator.Current => _current;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}