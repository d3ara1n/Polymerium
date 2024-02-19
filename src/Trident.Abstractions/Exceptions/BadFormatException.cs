namespace Trident.Abstractions.Exceptions
{
    public class BadFormatException : Exception
    {
        public BadFormatException(string file, int line, int column) : base(
            $"File({file}) has been parsed failed at line {line} column {column}")
        {
        }

        public BadFormatException(string file, string missingElement) : base(
            $"File({file}) has been parsed but missing element {missingElement}")
        {
        }

        public BadFormatException(string message) : base(message)
        {
        }

        public BadFormatException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        public BadFormatException()
        {
        }
    }
}