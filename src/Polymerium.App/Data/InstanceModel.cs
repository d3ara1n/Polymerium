using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions;

namespace Polymerium.App.Data
{
    public class InstanceModel : RefinedModelBase<GameInstance>
    {
        // 从 GameInstance 转换过来，用于数据保存读取
        public override Uri Location { get; } = new Uri("poly-file://instance.json", UriKind.Absolute);

        public static InstanceModel Create(GameInstance instance)
        {

            throw new NotImplementedException();
        }

        public override GameInstance Extract()
        {
            throw new NotImplementedException();
        }
    }
}
