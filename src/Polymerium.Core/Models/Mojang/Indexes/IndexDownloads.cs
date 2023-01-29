namespace Polymerium.Core.Models.Mojang.Indexes
{
    public struct IndexDownloads
    {
        public IndexDownloadsItem Client { get; set; }
        public IndexDownloadsItem ClientMappings { get; set; }
        public IndexDownloadsItem Server { get; set; }
        public IndexDownloadsItem ServerMappings { get; set; }
    }
}