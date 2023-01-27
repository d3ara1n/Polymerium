using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Engines.Restoring
{
    public enum RestoreError
    {
        ResourceNotReacheable,
        ResourceNotFound,
        SerializationFailure,
        OsNotSupport,
        Canceled,
        ExceptionOccurred,
        Unknown
    }
}
