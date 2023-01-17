using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.App.Data;

namespace Polymerium.App.Services
{
    public class DataCenter
    {
        public bool Save<T>(RefinedModelBase<T> model)
        {
            // 目前不用这个 DataCenter 来额外包装
            throw new NotImplementedException();
        }
    }
}
