using Polymerium.Trident;

namespace Polymerium.App.Services.States;

public record StateTracklet(string Key, InstanceState State) : ITracklet;