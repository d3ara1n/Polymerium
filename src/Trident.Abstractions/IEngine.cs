namespace Trident.Abstractions;

public interface IEngine<out TProd> : IEnumerable<TProd>
{
}