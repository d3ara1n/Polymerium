using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Polymerium.App.Configurations
{
    public class AppSettings
    {
        public bool ForceImportOffline
        {
            get =>
                (bool?)ApplicationData.Current.LocalSettings.Values[nameof(ForceImportOffline)]
                == true;
            set => ApplicationData.Current.LocalSettings.Values[nameof(ForceImportOffline)] = value;
        }
        public bool IsSuperPowerActivated
        {
            get =>
                (bool?)ApplicationData.Current.LocalSettings.Values[nameof(IsSuperPowerActivated)]
                == true;
            set =>
                ApplicationData.Current.LocalSettings.Values[nameof(IsSuperPowerActivated)] = value;
        }
    }
}
