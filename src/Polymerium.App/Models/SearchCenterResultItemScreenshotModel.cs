namespace Polymerium.App.Models;

public class SearchCenterResultItemScreenshotModel
{
    public SearchCenterResultItemScreenshotModel(string title, string url)
    {
        Title = title;
        Url = url;
    }

    public string Title { get; set; }
    public string Url { get; set; }
}
