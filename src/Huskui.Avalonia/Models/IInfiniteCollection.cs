namespace Huskui.Avalonia.Models;

public interface IInfiniteCollection
{
    bool HasNext { get; }
    bool IsFetching { get; }
    Task FetchAsync();
}