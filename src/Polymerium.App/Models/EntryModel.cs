using System.Windows.Input;

namespace Polymerium.App.Models
{
    public record EntryModel(string Key, string Title, string Category, string Thumbnail, ICommand GotoDetailCommand);
}
