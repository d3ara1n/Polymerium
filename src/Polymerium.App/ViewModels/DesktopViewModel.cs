using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using NanoidDotNet;
using Polymerium.App.Extensions;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Extracting;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class DesktopViewModel : ViewModelBase
{
    private readonly ModpackExtractor _extractor;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notification;
    private readonly ProfileManager _profileManager;
    private readonly StorageManager _storageManager;

    public DesktopViewModel(NavigationService navigation, ProfileManager profileManager, ModpackExtractor extractor,
        NotificationService notification, StorageManager storageManager)
    {
        _navigation = navigation;
        _extractor = extractor;
        _notification = notification;
        _profileManager = profileManager;
        _storageManager = storageManager;
        GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);

        Entries = profileManager.Managed.Select(x =>
                new EntryModel(x.Key, x.Value.Value, InstanceState.Idle, GotoInstanceViewCommand))
            .OrderByDescending(x => x.LastPlayAtRaw).ToList();
    }

    public IEnumerable<EntryModel> Entries { get; private set; }

    public RelayCommand<string> GotoInstanceViewCommand { get; }

    private void GotoInstanceView(string? key)
    {
        if (key != null)
            _navigation.Navigate(typeof(InstanceView), key, new DrillInNavigationTransitionInfo());
    }

    public FlattenExtractedContainer? ExtractModpack(string path)
    {
        var result = _extractor.ExtractAsync(path, null).GetAwaiter().GetResult();
        if (result.IsSuccessful) return result.Value;

        _notification.Enqueue($"Invalid input {Path.GetFileName(path)}: {result.Error.ToString()}");
        return null;
    }

    public void ApplyExtractedModpack(ModpackPreviewModel model)
    {
        var key = _profileManager.RequestKey(model.InstanceName);
        var layers = new List<Metadata.Layer>();

        var metadata = new Metadata(model.Version, layers);
        foreach (var item in model.Inner.Layers)
        {
            var loaders = new List<Loader>();

            loaders.AddRange(item.Original.Loaders);

            if (item.SolidFiles.Any())
            {
                var id = Nanoid.Generate(size: 11);
                var storageKey = _storageManager.RequestKey($"{key}.{id}");
                var storage = _storageManager.Open(storageKey);
                storage.EnsureEmpty();
                foreach (var file in item.SolidFiles)
                    storage.Write(file.FileName, file.Data.Span);
                var storageLoader = new Loader(Loader.COMPONENT_BUILTIN_STORAGE, storageKey);
                loaders.Add(storageLoader);
            }

            var attachments = item.Original.Attachments.Select(x => x.ToPurl()).ToList();
            var layer = new Metadata.Layer(null, true, item.Original.Summary, loaders, attachments);
            layers.Add(layer);
        }

        _profileManager.Create(key, model.InstanceName, null, null, metadata);
    }
}