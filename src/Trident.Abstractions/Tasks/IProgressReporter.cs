namespace Trident.Abstractions.Tasks;

public interface IProgressReporter : IProgress<double?>, IProgress<string>, IDisposable
{
}