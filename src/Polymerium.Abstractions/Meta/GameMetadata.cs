using System;
using System.Collections.Generic;

namespace Polymerium.Abstractions.Meta;

public struct GameMetadata
{
    public IEnumerable<Component> Components { get; set; }
    public IEnumerable<Uri> Attachments { get; set; }
}