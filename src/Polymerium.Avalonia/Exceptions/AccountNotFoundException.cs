using System;
using TridentCore.Core.Exceptions;

namespace Polymerium.Avalonia.Exceptions;

public class AccountNotFoundException(string message, Exception? inner = null)
    : AccountException(message, inner)
{ }