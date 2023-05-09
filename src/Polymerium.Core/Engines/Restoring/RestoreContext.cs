using Polymerium.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Engines.Restoring
{
    public class RestoreContext
    {
        public PolylockData Polylock { get; }

        public RestoreContext(PolylockData polylock)
        {
            Polylock = polylock;
        }
    }
}
