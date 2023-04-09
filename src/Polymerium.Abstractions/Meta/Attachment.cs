using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.Meta
{
    public struct Attachment
    {
        public Uri Source { get; set; }
        public Uri? From { get; set; }
    }
}
