namespace Polymerium.App.Models;

public class InstanceScreenshotModel
{
    public InstanceScreenshotModel(string fileName)
    {
        FileName = fileName;
    }

    public string FileName { get; set; }
}
