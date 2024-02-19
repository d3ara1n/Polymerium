namespace Trident.Abstractions.Errors
{
    public enum ExtractError
    {
        Unknown,
        Cancelled,
        BadFormat,
        BadStream,
        FileNotFound,
        ItemNotFound,
        TooLarge,
        Unsupported,
        Exception
    }
}