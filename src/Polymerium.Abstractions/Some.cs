using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions
{
    public class Some<T> : Option<T>
    {
        public T Value { get; private set; }
        public Some(T data)
        {
            Value = data;
        }
    }
}
