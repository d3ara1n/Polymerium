using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions
{
    public class Err<TOk, TErr> : Result<TOk, TErr>
    {
        public TErr Value { get; private set; }
        public Err(TErr err)
        {
            Value = err;
        }
    }

    public class Err<TErr> : Result<TErr>
    {
        public TErr Value { get; private set; }
        public Err(TErr err)
        {
            Value = err;
        }
    }
}
