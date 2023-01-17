using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Data
{
    public abstract class RefinedModelBase<T>
    {
        public abstract Uri Location { get; }
        public abstract T Extract();
    }
}
