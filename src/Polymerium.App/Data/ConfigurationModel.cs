using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.App.Configurations;

namespace Polymerium.App.Data
{
    public class ConfigurationModel : RefinedModelBase<Configuration>
    {
        private readonly Uri location = new Uri("poly-file:///configuration.json");
        public override Uri Location => location;

        public AppSettings Settings { get; set; }
        public string AccountShowcase { get; set; }

        public override void Apply(Configuration data)
        {
            Settings = data.Settings;
            AccountShowcase = data.AccountShowcaseId;
        }

        public override Configuration Extract()
        {
            var cfg = new Configuration()
            {
                Settings = Settings,
                AccountShowcaseId = AccountShowcase
            };
            return cfg;
        }
    }
}
