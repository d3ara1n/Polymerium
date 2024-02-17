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
    private readonly IHttpClientFactory _factory;
    private readonly InstanceManager _instanceManager;
    private readonly InstanceStatusService _instanceStatusService;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notification;
    private readonly ProfileManager _profileManager;
    private readonly ThumbnailSaver _thumbnailSaver;

    public DesktopViewModel(NavigationService navigation, ProfileManager profileManager, ModpackExtractor extractor,
        NotificationService notification, ThumbnailSaver thumbnailSaver, IHttpClientFactory factory,
        InstanceStatusService instanceStatusService, InstanceManager instanceManager)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _dispatcher.EnsureSystemDispatcherQueue();
        _navigation = navigation;
        _profileManager = profileManager;
        _extractor = extractor;
        _notification = notification;
        _thumbnailSaver = thumbnailSaver;
        _factory = factory;
        _instanceStatusService = instanceStatusService;
        _instanceManager = instanceManager;

        LaunchEntryCommand = new RelayCommand<EntryModel>(LaunchEntry, CanLaunchEntry);
        DeleteEntryCommand = new RelayCommand<EntryModel>(DeleteEntry, CanDeleteEntry);
        GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);

        Entries = new ObservableCollection<EntryModel>(profileManager.Managed.Select(x =>
                new EntryModel(x.Key, x.Value.Value, _thumbnailSaver.Get(x.Key), _instanceStatusService.MustHave(x.Key),
                    LaunchEntryCommand,
                    GotoInstanceViewCommand))
            .OrderByDescending(x => x.LastPlayAtRaw));

        IsActive = true;
    }

    public ObservableCollection<EntryModel> Entries { get; }

    private RelayCommand<EntryModel> LaunchEntryCommand { get; }
    private RelayCommand<EntryModel> DeleteEntryCommand { get; }
    private RelayCommand<string> GotoInstanceViewCommand { get; }

    public void Receive(ProfileAddedMessage message)
    {
        if (IsActive)
            _dispatcher.TryEnqueue(() =>
            {
                var status = _instanceStatusService.MustHave(message.Key);
                Entries.Add(new EntryModel(message.Key, message.Item, _thumbnailSaver.Get(message.Key), status,
                    LaunchEntryCommand,
                    GotoInstanceViewCommand));
            });
    }

    private void GotoInstanceView(string? key)
    {
        if (key != null)
            _navigation.Navigate(typeof(InstanceView), key, new DrillInNavigationTransitionInfo());
    }

    private bool CanDeleteEntry(EntryModel? entry)
    {
        return entry is { Status.State.Value: InstanceState.Idle or InstanceState.Stopped };
    }

    private void DeleteEntry(EntryModel? entry)
    {
        if (entry != null)
        {
        }
    }

    private bool CanLaunchEntry(EntryModel? entry)
    {
        return entry is { Status.State.Value: InstanceState.Idle or InstanceState.Stopped };
    }

    private void LaunchEntry(EntryModel? entry)
    {
        if (entry != null)
            // TODO: Create token for interrupt
            _instanceManager.Deploy(entry.Key, entry.Inner.Metadata, null, App.Current.Token);
    }

    public FlattenExtractedContainer? ExtractModpack(string path)
    {
        using var stream = File.OpenRead(path);
        var result = _extractor.ExtractAsync(stream, null).GetAwaiter().GetResult();
        if (result.IsSuccessful) return result.Value;

        _notification.PopInformation($"Invalid input {Path.GetFileName(path)}: {result.Error}");
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
        _notification.PopInformation($"{instanceName}({version}) has been added");
    }
}