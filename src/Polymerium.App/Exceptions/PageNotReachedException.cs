using System;

namespace Polymerium.App.Exceptions;

public class PageNotReachedException(Type page, string message) : NavigationFailedException(page, message)
{
}