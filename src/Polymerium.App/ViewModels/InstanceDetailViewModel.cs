using Polymerium.Trident;
using Polymerium.Trident.Data;
using Polymerium.Trident.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels
{
    using Model = (Entry, Handle<Profile>);

    public class InstanceDetailViewModel(EntryManager entryManager) : ViewModelBase
    {
        public Model? Model { get; private set; }

        public override bool OnAttached(object? maybeKey)
        {
            if (maybeKey is string key && key != null)
            {
                var entry = entryManager.Entries.FirstOrDefault(x => x.Key == key);
                if (entry == null) return false;
                var profile = entryManager.GetProfile(key);
                if (profile == null) return false;
                Model = (entry, profile);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void OnDetached()
        {
            if (Model.HasValue)
            {
                Model.Value.Item2.Dispose();
            }
        }
    }
}
