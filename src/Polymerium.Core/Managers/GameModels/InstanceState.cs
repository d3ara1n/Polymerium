using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Managers.GameModels
{
    public enum InstanceState
    {
        //
        Idle,

        // 还原中
        Preparing,

        // 还原完成
        Ready,

        // 运行中
        Running
    }
}
