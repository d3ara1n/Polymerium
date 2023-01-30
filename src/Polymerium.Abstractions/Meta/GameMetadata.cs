using System;
using System.Collections.Generic;

namespace Polymerium.Abstractions.Meta;

public struct GameMetadata
{
    public string LockFileSha1 { get; set; }
    public IEnumerable<Component> Components { get; set; }
    public IEnumerable<Uri> Attachments { get; set; }
}