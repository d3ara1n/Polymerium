using Polymerium.App.Services;
using Polymerium.Trident;
using Polymerium.Trident.Data;
using Polymerium.Trident.Managers;
using System.Linq;
using Trident.Abstractions;

namespace Polymerium.App.ViewModels
{
    using Model = (Entry, Handle<Profile>);

    public class InstanceDetailViewModel : ViewModelBase
    {
        private EntryManager _entryManager;

        public InstanceDetailViewModel(EntryManager entryManager, NavigationService navigation)
        {
            _entryManager = entryManager;
        }

        public Model? Model { get; private set; }

        public override bool OnAttached(object? maybeKey)
        {
            if (maybeKey is string key && key != null)
            {
                var entry = _entryManager.Entries.FirstOrDefault(x => x.Key == key);
                if (entry == null) return false;
                var profile = _entryManager.GetProfile(key);
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
