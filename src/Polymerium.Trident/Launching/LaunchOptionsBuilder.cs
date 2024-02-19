using IBuilder;
using System.Drawing;

namespace Polymerium.Trident.Launching
{
    public class LaunchOptionsBuilder : IBuilder<LaunchOptions>
    {
        private string additionalArguments = string.Empty;
        private JavaHomeLocatorDelegate javaHomeLocator = v => throw new JavaNotFoundException(v);
        private LaunchMode launchMode = LaunchMode.Managed;
        private uint maxMemory = 4096;
        private Size windowSize = new(1270, 720);

        public LaunchOptions Build()
        {
            LaunchOptions result = new(launchMode, windowSize, maxMemory, additionalArguments, javaHomeLocator);
            return result;
        }

        public LaunchOptionsBuilder WithMode(LaunchMode mode)
        {
            launchMode = mode;
            return this;
        }

        public LaunchOptionsBuilder WithMaxMemory(uint max)
        {
            maxMemory = max;
            return this;
        }

        public LaunchOptionsBuilder WithWindowSize(Size size)
        {
            windowSize = size;
            return this;
        }

        public LaunchOptionsBuilder WithAdditionalArguments(string argument)
        {
            additionalArguments = argument;
            return this;
        }

        public LaunchOptionsBuilder WithJavaHomeLocator(JavaHomeLocatorDelegate locator)
        {
            javaHomeLocator = locator;
            return this;
        }
    }
}