using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Polymerium.App.Assets;
using Polymerium.App.Models;

namespace Polymerium.App.DesignModels;

public class DesignExhibitModel() : ExhibitModel("curseforge", null, "114514", "Project Zero", "In Design", new Bitmap(AssetLoader.Open(new Uri(AssetUriIndex.DIRT_IMAGE))), "Me", ["Never", "Gonna", "Give", "You", "Up"], DateTimeOffset.Now, 1919810UL, new Uri("https://example.com"));