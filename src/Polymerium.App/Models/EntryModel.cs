using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Polymerium.App.Models
{
    public record EntryModel(string Key, string Title, string Category, string Thumbnail, string Summary,bool IsLiked, ICommand GotoDetailCommand);
}
