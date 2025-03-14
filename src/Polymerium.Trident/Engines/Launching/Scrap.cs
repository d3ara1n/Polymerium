namespace Polymerium.Trident.Engines.Launching;

public record Scrap(ScrapLevel Level, DateTimeOffset Time, string Thread, string? Sender, string Message);