using System;

namespace Polymerium.Abstractions.Meta;

public struct Attachment
{
    public Uri Source { get; set; }
    public Uri? From { get; set; }
}