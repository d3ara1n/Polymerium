using HtmlAgilityPack;
using Humanizer;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Polymerium.App.Helpers;
using Polymerium.Trident.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record ModpackModel(Project Inner)
    {
        public static readonly ModpackModel DUMMY =
            new(
                new Project(string.Empty, string.Empty, RepositoryLabels.VOID, null, string.Empty, string.Empty,
                    new Uri("https://example.com"),
                    ResourceKind.Mod, DateTimeOffset.Now, DateTimeOffset.Now, 0, string.Empty,
                    Enumerable.Empty<Project.Screenshot>(), Enumerable.Empty<Project.Version>()));

        public string Thumbnail => Inner.Thumbnail?.AbsoluteUri ?? "/Assets/Placeholders/default_dirt.png";
        public string CreatedAt => Inner.CreatedAt.Humanize();
        public string UpdatedAt => Inner.UpdatedAt.Humanize();
        public string DownloadCount => ((int)Inner.DownloadCount).ToMetric(decimals: 2);

        public ICollection<GalleryItemModel> Gallery =>
            Inner.Gallery.Select(x => new GalleryItemModel(x.Title, x.Url)).ToList();

        public ICollection<ModpackVersionModel> Versions =>
            Inner.Versions.Select(x => new ModpackVersionModel(x, this)).ToList();

        public RichTextBlock Description
        {
            get
            {
                string? html = HtmlEntity.DeEntitize(Inner.DescriptionHtml);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                Paragraph paragraph = new Paragraph();
                HtmlHelper.ParseNodeToInline(paragraph.Inlines, doc.DocumentNode,
                    Inner.Reference.AbsoluteUri);
                RichTextBlock block = new RichTextBlock();
                block.Blocks.Add(paragraph);
                return block;
            }
        }
    }
}