namespace Polymerium.Trident.Exceptions
{
    public class AccountAuthenticationException : Exception
    {
        public AccountAuthenticationException(string message, Exception? inner = null) : base(message, inner) { }
    }
}