using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Mojang.Indexes
{
    public struct Rule
    {
        public string Action { get; set; }
        public RuleFeatures? Features { get; set; }
        public RuleOs? Os { get; set; }
    }
}
