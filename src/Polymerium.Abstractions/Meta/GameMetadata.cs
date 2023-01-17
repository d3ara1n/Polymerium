using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.Meta
{
    // 这里得数据和游戏有关，意味着游戏本身，可以凭借该数据构造一个新的实例
    public struct GameMetadata
    {
        public string CoreVersion { get; set; }
        public IEnumerable<ExperienceExtender> Extenders { get; set; }
        public IEnumerable<AssetAttachments> Assets { get; set; }
    }
}
