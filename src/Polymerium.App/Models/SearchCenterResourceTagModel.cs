using Polymerium.Abstractions.Resources;

namespace Polymerium.App.Models;

public class SearchCenterResourceTagModel
{
    public SearchCenterResourceTagModel(ResourceType tag, string display)
    {
        Tag = tag;
        Display = display;
    }

    public ResourceType Tag { get; set; }
    public string Display { get; set; }
}