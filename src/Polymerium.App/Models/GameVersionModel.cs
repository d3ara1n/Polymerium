namespace Polymerium.App.Models;

public class GameVersionModel
{
    public GameVersionModel(string id, string releaseType)
    {
        Id = id;
        ReleaseType = releaseType;
    }

    public string Id { get; set; }
    public string ReleaseType { get; set; }
}
