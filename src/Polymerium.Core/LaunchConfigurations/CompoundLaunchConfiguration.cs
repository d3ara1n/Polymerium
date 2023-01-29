using Polymerium.Abstractions.LaunchConfigurations;
using System;

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

        public override string JavaPath { get => _first.JavaPath ?? _second.JavaPath; set => throw new NotSupportedException(); }
        public override uint MaxJvmMemory { get => _first.MaxJvmMemory != default ? _first.MaxJvmMemory : _second.MaxJvmMemory; set => throw new NotSupportedException(); }
        public override uint WindowHeight { get => _first.WindowHeight != default ? _first.WindowHeight : _second.WindowHeight; set => throw new NotSupportedException(); }
        public override uint WindowWidth { get => _first.WindowWidth != default ? _first.WindowWidth : _second.WindowWidth; set => throw new NotSupportedException(); }
    }
}