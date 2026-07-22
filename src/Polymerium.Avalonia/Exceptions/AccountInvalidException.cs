using System;
using TridentCore.Core.Exceptions;

namespace Polymerium.Avalonia.Exceptions;

public class AccountInvalidException(string message, Exception? inner = null) : AccountException(message, inner) { }
