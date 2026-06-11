using System;

namespace Polymerium.Avalonia.Exceptions;

public class AccountInvalidException(string message, Exception? inner = null)
    : Exception(message, inner)
{ }
