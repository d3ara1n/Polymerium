using Polymerium.Trident.Exceptions;
using Trident.Abstractions;

namespace Polymerium.Trident.Igniters;

public class LaunchOptionsBuilder : IBuilder.IBuilder<LaunchOptions>
{
    private string _additionalArguments = string.Empty;
    private JavaHomeLocatorDelegate _javaHomeLocator = v => throw new JavaNotFoundException(v);
    private LaunchMode _launchMode = LaunchMode.Managed;
    private uint _maxMemory = 4096;
    private (uint, uint) _windowSize = (1270, 720);

    public LaunchOptions Build()
    {
        LaunchOptions result = new(_launchMode, _windowSize, _maxMemory, _additionalArguments, _javaHomeLocator);
        return result;
    }

    public LaunchOptionsBuilder WithMode(LaunchMode mode)
    {
        _launchMode = mode;
        return this;
    }

    public LaunchOptionsBuilder WithMaxMemory(uint max)
    {
        _maxMemory = max;
        return this;
    }

    public LaunchOptionsBuilder WithWindowSize((uint, uint) size)
    {
        _windowSize = size;
        return this;
    }

    public LaunchOptionsBuilder WithAdditionalArguments(string argument)
    {
        _additionalArguments = argument;
        return this;
    }

    public LaunchOptionsBuilder WithJavaHomeLocator(JavaHomeLocatorDelegate locator)
    {
        _javaHomeLocator = locator;
        return this;
    }
}