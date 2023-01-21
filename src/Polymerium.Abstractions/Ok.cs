using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions
{
    public class Ok<TOk, TErr> : Result<TOk, TErr>
    {
        public TOk Value { get; private set; }

        public Ok(TOk value)
        {
            Value = value;
        }
    }

    public class Ok<TErr> : Result<TErr> { }
}
