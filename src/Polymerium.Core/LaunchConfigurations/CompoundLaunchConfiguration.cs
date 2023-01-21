using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions.LaunchConfigurations;

namespace Polymerium.Core.LaunchConfigurations
{
    public class CompoundLaunchConfiguration : LaunchConfigurationBase
    {
        private readonly LaunchConfigurationBase _first;
        private readonly LaunchConfigurationBase _second;
        public CompoundLaunchConfiguration(LaunchConfigurationBase first, LaunchConfigurationBase second)
        {
            _first = first;
            _second = second;
        }
    }
}
