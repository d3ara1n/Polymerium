using System;

namespace Polymerium.App.Services.States;

public record ExceptionTracklet(string Key, Exception Error) : ITracklet;