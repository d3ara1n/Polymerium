using System;

namespace Polymerium.App.Exceptions;

public class AccountInvalidException(string message, Exception? inner = null) : Exception(message, inner) { }
