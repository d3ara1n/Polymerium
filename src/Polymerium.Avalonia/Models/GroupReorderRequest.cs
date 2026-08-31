using System.Collections.Generic;

namespace Polymerium.Avalonia.Models;

public sealed record GroupReorderRequest(IReadOnlyList<string> Sources);
