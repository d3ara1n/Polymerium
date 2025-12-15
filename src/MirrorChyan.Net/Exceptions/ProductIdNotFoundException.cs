namespace MirrorChyan.Net.Exceptions;

public class ProductIdNotFoundException(string message, string productId) : Exception(message)
{
    public string ProductId => productId;
}
