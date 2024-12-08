using Avalonia.Threading;

namespace Huskui.Avalonia;

public interface IPageModel
{
    Task InitializeAsync(Dispatcher dispatcher, CancellationToken token = default);
    Task CleanupAsync(CancellationToken token = default);
}