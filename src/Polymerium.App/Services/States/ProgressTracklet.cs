namespace Polymerium.App.Services.States;

public record ProgressTracklet(string Key, double? Progress) : ITracklet;