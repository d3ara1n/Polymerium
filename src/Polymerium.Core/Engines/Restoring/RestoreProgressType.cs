using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Engines.Restoring
{
    public enum RestoreProgressType
    {
        Core,
        Libraries,
        Assets,
        Download,
        ErrorOccurred,
        AllCompleted
    }
}
