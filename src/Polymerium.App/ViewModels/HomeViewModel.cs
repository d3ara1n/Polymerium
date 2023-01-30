using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class HomeViewModel : ObservableObject
{
    public HomeViewModel(MemoryStorage memoryStorage)
    {
        MemoryStorage = memoryStorage;
        RecentPlays = new ObservableCollection<RecentPlayedItemModel>
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

    public ObservableCollection<RecentPlayedItemModel> RecentPlays { get; }
    public MemoryStorage MemoryStorage { get; }
}