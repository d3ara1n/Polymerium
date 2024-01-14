using System.Linq;
using HtmlAgilityPack;
using Humanizer;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Polymerium.App.Helpers;
using Trident.Abstractions.Resources;
using static Trident.Abstractions.Metadata.Layer;

namespace Polymerium.App.Models;

public record ProjectVersionModel(Project.Version Inner, ProjectModel Model)
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
            var html = HtmlEntity.DeEntitize(Inner.ChangelogHtml);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var paragraph = new Paragraph();
            HtmlHelper.ParseNodeToInline(paragraph.Inlines, doc.DocumentNode,
                Model.Inner.Reference.AbsoluteUri);
            var block = new RichTextBlock();
            block.Blocks.Add(paragraph);
            return block;
        }
    }
}