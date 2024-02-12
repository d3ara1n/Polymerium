namespace Trident.Abstractions.Tasks;

public delegate Task TrackerHandler(TrackerBase handle, CancellationToken token);