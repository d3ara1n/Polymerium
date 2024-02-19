using Polymerium.Trident.Engines.Launching;
using Trident.Abstractions;

namespace Polymerium.Trident.Engines
{
    public class LaunchEngine : IAsyncEngine<Scrap>
    {
        public IAsyncEnumerator<Scrap> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}