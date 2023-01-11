using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions
{
    public class None<T> : Option<T>
    {
        internal None() { }
    }
}
