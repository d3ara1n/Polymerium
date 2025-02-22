namespace Huskui.Avalonia;

public interface IPageModel
{
    Task InitializeAsync(CancellationToken token = default);
    Task CleanupAsync(CancellationToken token = default);
}