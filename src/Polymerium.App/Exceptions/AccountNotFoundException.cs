using System;

namespace Polymerium.App.Exceptions;

public class AccountNotFoundException(string message, Exception? inner = null) : Exception(message, inner) { }