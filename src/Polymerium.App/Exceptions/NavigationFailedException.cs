using System;

namespace Polymerium.App.Exceptions;

public class NavigationFailedException(Type page, string message) : Exception(message)
{
    public Type Page => page;
}
