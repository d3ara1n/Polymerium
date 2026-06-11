using System;

namespace Polymerium.Avalonia.Exceptions;

public class AccountNotFoundException(string message, Exception? inner = null)
    : Exception(message, inner)
{ }
