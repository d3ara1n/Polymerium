using Polymerium.Abstractions;
using System.Collections.Generic;

namespace Polymerium.App.Models
{
    // TODO: 在修改完 GameInstance 后应该更新这些值
    public class InstanceModel
    {
        public GameInstance Inner { get; set; }
        public string InstanceName => Inner.Name;
        public IEnumerable<ComponentTagItemModel> Extenders { get; set; }
    }
}