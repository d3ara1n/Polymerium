namespace Trident.Abstractions;

public interface IAsyncEngine<out TProd> : IAsyncEnumerable<TProd>
{
}