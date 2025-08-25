namespace Polymerium.Trident.Models.MojangLauncherApi
{
    public readonly record struct MinecraftNewsResponse(int Version, IReadOnlyList<MinecraftNewsResponse.Entry> Entries)
    {
        #region Nested type: Entry

        public readonly record struct Entry(
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
            #region Nested type: EntryImage

            public readonly record struct EntryImage(string Title, Uri Url, EntryImage.ImageSize? Dimension)
            {
                #region Nested type: ImageSize

                public record ImageSize(uint Width, uint Height);

                #endregion
            }

            #endregion
        }

        #endregion
    }
}
