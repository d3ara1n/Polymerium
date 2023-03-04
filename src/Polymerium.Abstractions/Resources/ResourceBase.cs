using System;

namespace Polymerium.Abstractions.Resources;

public abstract record ResourceBase
{
    protected ResourceBase(string id, string name, string author, Uri iconSource, string summary)
    {
        Id = id;
        Name = name;
        Author = author;
        IconSource = iconSource;
        Summary = summary;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public Uri IconSource { get; set; }
    public string Summary { get; set; }
}