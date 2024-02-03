namespace Trident.Abstractions.Profiles;

public record TimelinePoint(
    bool Success,
    string? Source,
    TimelimeAction Action,
    DateTimeOffset BeginTime,
    DateTimeOffset EndTime)
{
}