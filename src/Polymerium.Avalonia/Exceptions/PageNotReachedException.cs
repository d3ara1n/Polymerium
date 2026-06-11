using System;

namespace Polymerium.Avalonia.Exceptions;

public class PageNotReachedException(Type page, string message)
    : NavigationFailedException(page, message)
{ }
