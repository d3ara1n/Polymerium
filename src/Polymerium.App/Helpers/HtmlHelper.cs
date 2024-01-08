using HtmlAgilityPack;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace Polymerium.App.Helpers
{
    public static class HtmlHelper
    {
        public static void ParseNodeToInline(InlineCollection collection, HtmlNode node, string reference, bool ignoreText = false)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Element:
                    var append = true;
                    switch (node.Name.ToLower())
                    {
                        case "img":
                            {
                                var src = node.GetAttributeValue("src", "https://placehold.co/400x200");
                                var width = node.GetAttributeValue("width", 400);
                                var height = node.GetAttributeValue("height", 200);
                                if (Uri.IsWellFormedUriString(src, UriKind.Absolute))
                                    collection.Add(new InlineUIContainer()
                                    {
                                        Child = new Image()
                                        {
                                            Source = new BitmapImage(new Uri(src, UriKind.Absolute)),
                                            Width = width,
                                            Height = height
                                        }
                                    });
                            }
                            break;
                        case "a":
                            {
                                var href = node.GetAttributeValue("href", reference);
                                if (Uri.IsWellFormedUriString(href, UriKind.Absolute))
                                {
                                    var run = new Run() { Text = string.IsNullOrEmpty(node.InnerText) ? "[Link]" : node.InnerText };
                                    var link = new Hyperlink()
                                    {
                                        NavigateUri = new Uri(href, UriKind.Absolute)
                                    };
                                    link.Inlines.Add(run);
                                    collection.Add(link);
                                    ignoreText = true;
                                }
                            }
                            break;
                        case "strong":
                            {
                                var bold = new Bold();
                                bold.Inlines.Add(new Run()
                                {
                                    Text = node.InnerText
                                });
                                collection.Add(bold);
                                ignoreText = true;
                            }
                            break;
                        case "iframe":
                            {
                                var src = node.GetAttributeValue("src", reference);
                                if (Uri.IsWellFormedUriString(src, UriKind.Absolute))
                                {
                                    var run = new Run() { Text = "[Embedded Video]" };
                                    var link = new Hyperlink()
                                    {
                                        NavigateUri = new Uri(src, UriKind.Absolute)
                                    };
                                    link.Inlines.Add(run);
                                    collection.Add(link);
                                }
                            }
                            break;
                        case "br":
                            collection.Add(new LineBreak());
                            break;
                        case "ul":
                            foreach (var child in node.ChildNodes)
                                ParseNodeToInline(collection, child, reference);
                            append = false;
                            break;
                        case "li":
                            {
                                var dot = new Bold();
                                dot.Inlines.Add(new Run() { Text = " · " });
                                collection.Add(dot);
                            }
                            break;
                        case "span":
                            break;
                        case "div":
                            break;
                        case "p":
                            break;
                        default:
                            collection.Add(new Run()
                            {
                                Text = node.InnerHtml
                            });
                            break;

                    }
                    if (append) foreach (var child in node.ChildNodes)
                            ParseNodeToInline(collection, child, reference, ignoreText);
                    break;
                case HtmlNodeType.Document:
                    foreach (var child in node.ChildNodes)
                    {
                        ParseNodeToInline(collection, child, reference);
                    }
                    break;
                case HtmlNodeType.Text:
                    if (!ignoreText) collection.Add(new Run() { Text = node.InnerText });
                    break;
            }
        }
    }
}
