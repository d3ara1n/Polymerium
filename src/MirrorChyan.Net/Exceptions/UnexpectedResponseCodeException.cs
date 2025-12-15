namespace MirrorChyan.Net.Exceptions;

public class UnexpectedResponseCodeException(string message, int code) : Exception(message)
{
    public int Code => code;
}
