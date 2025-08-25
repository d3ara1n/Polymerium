namespace Polymerium.Trident.Exceptions
{
    public class ProcessFaultedException(int exitCode, string message) : Exception(message)
    {
        public int ExitCode => exitCode;
    }
}
