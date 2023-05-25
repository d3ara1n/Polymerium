using System;

namespace Polymerium.Abstractions.Meta;

public class Attachment
{
    public Attachment(Uri source, Uri? from = null, bool enabled = true)
    {
        Source = source;
        Enabled = enabled;
        From = from;
    }

    public Uri Source { get; set; }
    public bool Enabled { get; set; }
    public Uri? From { get; set; }
}
