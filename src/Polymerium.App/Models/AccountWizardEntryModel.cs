using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public class AccountWizardEntryModel
    {
        public string Caption { get; set; }
        public string BrandIconSource { get; set; }
        public Type Page { get; set; }
    }
}
