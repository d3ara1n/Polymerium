using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;

namespace Polymerium.Core.Engines
{
    public class RestoreEngine
    {
        private readonly GameManager _manager;
        private readonly ResolveEngine _resolver;
        private readonly DownloadEngine _downloader;
        public RestoreEngine(GameManager manager, ResolveEngine resolver, DownloadEngine downloader)
        {
            _manager = manager;
            _resolver = resolver;
            _downloader = downloader;
        }
        public void Restore(GameInstance instance)
        {
            // 除了实例本身的 meta 用于原版和一些附件, 还需要共享的 assets/libraries 目录
            // 获取资源并用 manager 安装到实例的本地文件
        }
    }
}
