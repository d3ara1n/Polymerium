using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.App.Models
{
    public class InstancePackageDependencyCollection(
        uint refCount,
        uint strongRefCount,
        IList<InstancePackageDependencyModel> items) : Collection<InstancePackageDependencyModel>(items)
    {
        public uint RefCount => refCount;
        public uint StrongRefCount => strongRefCount;
    }
}
