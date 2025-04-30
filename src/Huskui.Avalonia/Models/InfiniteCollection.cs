using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Huskui.Avalonia.Models;

public class InfiniteCollection<T>(Func<int, CancellationToken, Task<IEnumerable<T>>> factory, int startIndex = 0)
    : ObservableCollection<T>, IInfiniteCollection, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private int _index = startIndex;

    public void Dispose()
    {
        if (!_cts.IsCancellationRequested)
            _cts.Cancel();
    }

    #region IInfiniteCollection Members

    public async Task FetchAsync()
    {
        if (IsFetching && _cts.IsCancellationRequested)
            return;

        IsFetching = true;
        var rv = await factory.Invoke(_index++, _cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3));
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
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasNext)));
        }
    } = true;

    public bool IsFetching
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsFetching)));
        }
    }

    #endregion
}