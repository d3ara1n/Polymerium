using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Huskui.Avalonia.Models;

public class InfiniteCollection<T>(Func<int, Task<IEnumerable<T>>> factory, int startIndex = 0)
    : ObservableCollection<T>, IInfiniteCollection
{
    private int _index = startIndex;
    private bool _hasNext = true;
    private bool _isFetching;

    public async Task FetchAsync()
    {
        if (IsFetching) return;
        IsFetching = true;
        var rv = await factory.Invoke(_index++);
        var dirty = false;
        foreach (var item in rv)
        {
            dirty = true;
            Add(item);
        }

        HasNext = dirty;
        IsFetching = false;
    }

    public bool HasNext
    {
        get => _hasNext;
        set
        {
            if (_hasNext == value) return;
            _hasNext = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasNext)));
        }
    }

    public bool IsFetching
    {
        get => _isFetching;
        set
        {
            if (_isFetching == value) return;
            _isFetching = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsFetching)));
        }
    }
}