using System;

namespace Polymerium.Core.Models.Mojang.Indexes
{
    public struct Rule
    {
        public string Action { get; set; }
        public RuleFeatures? Features { get; set; }
        public RuleOs? Os { get; set; }

        public bool Verfy()
        {
            if(Features.HasValue) return false;
            var matched = !Os.HasValue || Os.Value.Match();
            return Action.Equals("allow", StringComparison.OrdinalIgnoreCase) ? matched : !matched;
        }
    }
}