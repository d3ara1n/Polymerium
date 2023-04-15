using System;

namespace Polymerium.App.Models;

public class HomeNewsItemModel
{
    public HomeNewsItemModel(string title, string text, DateTimeOffset date, Uri? readMore, string imageSource)
    {
        Title = title;
        Text = text;
        Date = date;
        ReadMore = readMore;
        ImageSource = imageSource;
    }

    public string Title { get; set; }
    public string Text { get; set; }
    public DateTimeOffset Date { get; set; }
    public Uri? ReadMore { get; set; }
    public string ImageSource { get; set; }
}