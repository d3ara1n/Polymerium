namespace Polymerium.Trident.Exceptions;

public class AccountAuthenticationException(string message, Exception? inner = null) : Exception(message, inner)
{
}