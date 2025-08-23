namespace Polymerium.Trident.Exceptions
{
    public class JavaNotFoundException : Exception
    {
        public JavaNotFoundException(uint majorVersion) : base($"Jre version {majorVersion} not found") { }

        public JavaNotFoundException(string message, Exception? inner = null) : base(message, inner) { }
    }
}
