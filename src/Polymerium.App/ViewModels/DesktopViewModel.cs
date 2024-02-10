using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Messages;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Models.PrismLauncher.Minecraft;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Extracting;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class DesktopViewModel : ObservableRecipient, IRecipient<ProfileAddedMessage>
{
    private readonly DispatcherQueue _dispatcher;
    private readonly ModpackExtractor _extractor;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notification;
    private readonly ThumbnailSaver _thumbnailSaver;
    private readonly IHttpClientFactory _factory;
    private readonly ProfileManager _profileManager;

    public DesktopViewModel(NavigationService navigation, ProfileManager profileManager, ModpackExtractor extractor,
        NotificationService notification, ThumbnailSaver thumbnailSaver, IHttpClientFactory factory)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _navigation = navigation;
        _profileManager = profileManager;
        _extractor = extractor;
        _notification = notification;
        _thumbnailSaver = thumbnailSaver;
        _factory = factory;
        GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);

        Entries = new ObservableCollection<EntryModel>(profileManager.Managed.Select(x =>
                new EntryModel(x.Key, x.Value.Value, _thumbnailSaver.Get(x.Key), InstanceState.Idle,
                    GotoInstanceViewCommand))
            .OrderByDescending(x => x.LastPlayAtRaw));

        IsActive = true;
    }

    public ObservableCollection<EntryModel> Entries { get; }

    private RelayCommand<string> GotoInstanceViewCommand { get; }

    public void Receive(ProfileAddedMessage message)
    {
        _dispatcher.TryEnqueue(() =>
        {
            Entries.Add(new EntryModel(message.Key, message.Item, _thumbnailSaver.Get(message.Key), InstanceState.Idle,
                GotoInstanceViewCommand));
        });
    }

    private void GotoInstanceView(string? key)
    {
        if (key != null)
            _navigation.Navigate(typeof(InstanceView), key, new DrillInNavigationTransitionInfo());
    }

    public FlattenExtractedContainer? ExtractModpack(string path)
    {
        using var stream = File.OpenRead(path);
        var result = _extractor.ExtractAsync(stream, null).GetAwaiter().GetResult();
        if (result.IsSuccessful) return result.Value;

        _notification.Enqueue($"Invalid input {Path.GetFileName(path)}: {result.Error.ToString()}");
        return null;
    }

    public void ApplyExtractedModpack(ModpackPreviewModel model)
    {
        _extractor.SolidifyAsync(model.Inner, model.InstanceName).RunSynchronously();
    }

    public async Task<IEnumerable<MinecraftVersionModel>> FetchVersionAsync()
    {
        var manifest = await MinecraftHelper.GetManifestAsync(_factory);
        return manifest.Versions.Select(x => new MinecraftVersionModel(x.Version, x.Type switch
        {
            PrismMinecraftReleaseType.Release => ReleaseType.Release,
            PrismMinecraftReleaseType.Snapshot => ReleaseType.Snapshot,
            PrismMinecraftReleaseType.Old_Snapshot => ReleaseType.Snapshot,
            PrismMinecraftReleaseType.Experiment => ReleaseType.Experiment,
            PrismMinecraftReleaseType.Old_Alpha => ReleaseType.Alpha,
            PrismMinecraftReleaseType.Old_Beta => ReleaseType.Beta,
            _ => throw new NotImplementedException()
        }, x.ReleaseTime));
    }

    public async Task CreateProfileAsync(string instanceName, string version, MemoryStream? thumbnail)
    {
        var key = _profileManager.RequestKey(instanceName);
        if (thumbnail != null)
        {
            thumbnail.Position = 0;
            await _thumbnailSaver.SaveAsync(key.Key, thumbnail);
        }

        _profileManager.Append(key, instanceName, null, new Metadata(version, new List<Metadata.Layer>()));
        _notification.Enqueue($"{instanceName}({version}) has been added");
    }
}