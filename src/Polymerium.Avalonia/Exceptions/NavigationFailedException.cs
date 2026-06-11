using System;

namespace Polymerium.Avalonia.Exceptions;

public class NavigationFailedException(Type page, string message) : Exception(message)
{
    public Type Page => page;
}
