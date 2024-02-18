using HtmlAgilityPack;
using Humanizer;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Polymerium.App.Helpers;
using System.Linq;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record ModpackVersionModel(Project.Version Inner, ModpackModel Model)
    {
        public string RequiredAnyOfVersions => string.Join("·", Inner.Requirements.AnyOfVersions);

        public string RequiredAnyOfLoaders => string.Join("·",
            Inner.Requirements.AnyOfLoaders.Select(x =>
                Loader.MODLOADER_NAME_MAPPINGS.Keys.Contains(x) ? Loader.MODLOADER_NAME_MAPPINGS[x] : x));

        public string Labels => string.Join("·", RequiredAnyOfLoaders, RequiredAnyOfVersions);
        public string PublishedAt => Inner.PublishedAt.Humanize();

        public RichTextBlock Changelog
        {
            get
            {
                string? html = HtmlEntity.DeEntitize(Inner.ChangelogHtml);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                Paragraph paragraph = new Paragraph();
                HtmlHelper.ParseNodeToInline(paragraph.Inlines, doc.DocumentNode,
                    Model.Inner.Reference.AbsoluteUri);
                RichTextBlock block = new RichTextBlock();
                block.Blocks.Add(paragraph);
                return block;
            }
        }
    }
}