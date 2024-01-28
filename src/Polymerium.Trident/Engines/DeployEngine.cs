using System.Collections;
using Polymerium.Trident.Engines.Deploying;
using Trident.Abstractions;

namespace Polymerium.Trident.Engines;
// Deploy 包含以下过程：
// Build polylockdata:
//   Resolve attachments, Install game, Process loaders
// Solidify polylockdata:
//   Download libraries, Download & link attachments, Restore assets

// {watermark}.trident.json
// 水印是 Metadata 最后一次修改的时间
public class DeployEngine : IEngine<DeployContext, StageBase>
{
    public void SetContext(DeployContext fuel)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<StageBase> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}