namespace Polymerium.Trident.Models.MojangLauncherApi;

public record MinecraftNewsResponse(int Version, IReadOnlyList<MinecraftNewsResponse.Entry> Entries)
{
    public record Entry(
        string Title,
        string Category,
        DateTimeOffset Date,
        string Text,
        Entry.EntryImage PlayPageImage,
        Entry.EntryImage NewsPageImage,
        Uri ReadMoreLink,
        bool CardBorder,
        IReadOnlyList<string> NewsType,
        string Id)
    {
        public record EntryImage(string Title, Uri Url, EntryImage.ImageSize? Dimension)
        {
            public record ImageSize(uint Width, uint Height);
        }
    }
}