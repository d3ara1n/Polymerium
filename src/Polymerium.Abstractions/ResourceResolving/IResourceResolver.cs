using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.ResourceResolving
{
    // poly-res://<type>:domain/...
    //   poly-res://mod/<id>/<version>
    //     poly-res://mod:modrinth/<id>/<version>
    // 只有附件能用 url 表示并解析，游戏本体不能，同理 component 也是，都是由 DownloadSource 完成分流
    public interface IResourceResolver
    {
    }
}
