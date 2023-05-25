using System;

namespace Polymerium.App.Models;

public class InstanceConfigurationPageModel
{
    public InstanceConfigurationPageModel(string iconSource, string header, Type page)
    {
        IconSource = iconSource;
        Header = header;
        Page = page;
    }

    public string IconSource { get; set; }
    public string Header { get; set; }
    public Type Page { get; set; }
}
