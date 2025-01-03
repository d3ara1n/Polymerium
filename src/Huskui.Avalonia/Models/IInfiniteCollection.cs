namespace Huskui.Avalonia.Models;

public interface IInfiniteCollection
{
    Task FetchAsync();
    bool HasNext { get; }
    bool IsFetching { get; }
}