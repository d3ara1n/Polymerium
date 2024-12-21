using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class InstanceSourceModel(string source, string label, string url) : ModelBase
{
    public string Source => source;
    public string Label => label;
    public string Url => url;
}