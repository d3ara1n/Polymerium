using System;
using System.Collections.Generic;

namespace Polymerium.Abstractions.Meta;

public struct GameMetadata
{
    public IList<Component> Components { get; set; }
    public IList<Attachment> Attachments { get; set; }

    public GameMetadata()
    {
        Components = new List<Component>();
        Attachments = new List<Attachment>();
    }
}
