namespace Polymerium.Trident.Engines.Launching;

public record Scrap
{
    public Scrap(string message, ScrapLevel? level, DateTimeOffset? time, string? thread, string? sender)
    {
        Level = level;
        Time = time;
        Thread = thread;
        Sender = sender;
        Message = message;
    }

    public Scrap(string message) => Message = message;

    public string Message { get; init; }
    public ScrapLevel? Level { get; init; }
    public DateTimeOffset? Time { get; init; }
    public string? Thread { get; init; }
    public string? Sender { get; init; }
}