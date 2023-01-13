using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;

namespace Polymerium.App.ViewModels
{
    public class HomeViewModel : ObservableObject
    {
        public ObservableCollection<RecentPlayedItemModel> RecentPlays { get; private set; }
        public HomeViewModel()
        {
            RecentPlays = new ObservableCollection<RecentPlayedItemModel>()
            {
                new()
                {
                    Name = "All The Mods 8",
                    LastPlayedAt = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(9)
                },
                new()
                {
                    Name = "Pluma",
                    LastPlayedAt = DateTimeOffset.Parse("2023/1/1T13:59")
                },
                new()
                {
                    Name = "TNP Limitless 5",
                    LastPlayedAt = DateTimeOffset.Parse("2022/10/21T22:10")
                }
            };
        }
    }
}
