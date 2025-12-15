namespace MirrorChyan.Net.Exceptions;

public class CdkNotAvailableException(string message, string cdk) : Exception(message)
{
    public string Cdk => cdk;
}
