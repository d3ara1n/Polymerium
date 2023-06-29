using Polymerium.Abstractions.Models.Game;
using System.Collections.Generic;

namespace Polymerium.Abstractions.Models;

// 由 meta 结合实际 index.json 数据编译出来的完整的文件链。决定了实例的最终状态。
public struct PolylockData
{
    public string MainClass { get; set; }
    public AssetIndex AssetIndex { get; set; }
    public IEnumerable<Library> Libraries { get; set; }
    public IEnumerable<string> GameArguments { get; set; }
    public IEnumerable<string> JvmArguments { get; set; }
    public IEnumerable<PolylockAttachment> Attachments { get; set; }
    public IDictionary<string, string> Cargo { get; set; }
    public int JavaMajorVersionRequired { get; set; }
}
