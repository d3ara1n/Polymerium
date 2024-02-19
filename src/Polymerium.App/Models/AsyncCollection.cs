using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public class AsyncCollection<T>
        : ObservableCollection<T>
    {
        private readonly DispatcherQueue _dispatcher;
        private readonly CancellationToken _token;

        private Exception? exception;

        private DataLoadingState state = DataLoadingState.Loading;

        public AsyncCollection(IAsyncEnumerable<T> iter, CancellationToken token = default)
        {
            _token = token;
            _dispatcher = DispatcherQueue.GetForCurrentThread();

            Task.Run(async () => await LoadAsync(iter));
        }

        public DataLoadingState State
        {
            get => state;
            set
            {
                if (state != value)
                {
                    state = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(State)));
                }
            }
        }

        public Exception? Exception
        {
            get => exception;
            set
            {
                if (exception != null)
                {
                    exception = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Exception)));
                }
            }
        }

        private async Task LoadAsync(IAsyncEnumerable<T> iter)
        {
            State = DataLoadingState.Loading;
            try
            {
                await foreach (T handle in iter.WithCancellation(_token).ConfigureAwait(false))
                {
                    _dispatcher.TryEnqueue(() => { Add(handle); });
                }

                State = DataLoadingState.Done;
            }
            catch (Exception e)
            {
                Exception = e;
                State = DataLoadingState.Failed;
            }
        }
    }
}