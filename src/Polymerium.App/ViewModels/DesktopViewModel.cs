using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DotNext;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Messages;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Models.PrismLauncher;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Extracting;
using Polymerium.Trident.Services.Profiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Trident.Abstractions;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels
{
    public class DesktopViewModel : RecipientViewModelBase, IRecipient<ProfileAddedMessage>
    {
        private readonly DispatcherQueue _dispatcher;
        private readonly ModpackExtractor _extractor;
        private readonly IHttpClientFactory _factory;
        private readonly InstanceService _instanceService;
        private readonly InstanceStatusService _instanceStatusService;
        private readonly NavigationService _navigation;
        private readonly NotificationService _notification;
        private readonly ProfileManager _profileManager;
        private readonly ThumbnailSaver _thumbnailSaver;

        public DesktopViewModel(NavigationService navigation, ProfileManager profileManager, ModpackExtractor extractor,
            NotificationService notification, ThumbnailSaver thumbnailSaver, IHttpClientFactory factory,
            InstanceStatusService instanceStatusService, InstanceService instanceService)
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();
            _navigation = navigation;
            _profileManager = profileManager;
            _extractor = extractor;
            _notification = notification;
            _thumbnailSaver = thumbnailSaver;
            _factory = factory;
            _instanceStatusService = instanceStatusService;
            _instanceService = instanceService;

            LaunchEntryCommand = new RelayCommand<EntryModel>(LaunchEntry, CanManipulateEntry);
            DeployEntryCommand = new RelayCommand<EntryModel>(DeployEntry, CanManipulateEntry);
            GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);

            Entries = new ObservableCollection<EntryModel>(profileManager.Managed.Select(x =>
                    new EntryModel(x.Key, x.Value.Value, _thumbnailSaver.Get(x.Key),
                        _instanceStatusService.MustHave(x.Key),
                        LaunchEntryCommand,
                        DeployEntryCommand,
                        GotoInstanceViewCommand))
                .OrderByDescending(x => x.LastPlayAtRaw));
        }

        public ObservableCollection<EntryModel> Entries { get; }

        private RelayCommand<EntryModel> LaunchEntryCommand { get; }
        private RelayCommand<EntryModel> DeployEntryCommand { get; }
        private RelayCommand<string> GotoInstanceViewCommand { get; }

        public void Receive(ProfileAddedMessage message)
        {
            if (IsActive)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    InstanceStatusModel status = _instanceStatusService.MustHave(message.Key);
                    Entries.Add(new EntryModel(message.Key, message.Item, _thumbnailSaver.Get(message.Key), status,
                        LaunchEntryCommand,
                        DeployEntryCommand,
                        GotoInstanceViewCommand));
                });
            }
        }

        public override bool OnAttached(object? parameter)
        {
            IsActive = true;
            return base.OnAttached(parameter);
        }

        public override void OnDetached()
        {
            IsActive = false;
            base.OnDetached();
        }

        private void GotoInstanceView(string? key)
        {
            if (key != null)
            {
                _navigation.Navigate(typeof(InstanceView), key, new DrillInNavigationTransitionInfo());
            }
        }

        private bool CanManipulateEntry(EntryModel? entry)
        {
            if (entry != null)
            {
                return _instanceService.CanManipulate(entry.Key);
            }

            return false;
        }

        private void LaunchEntry(EntryModel? entry)
        {
            if (entry != null)
            {
                _instanceService.Launch(entry.Key);
            }
        }

        private void DeployEntry(EntryModel? entry)
        {
            if (entry != null)
            {
                _instanceService.LaunchSafelyBecauseThisIsUiPackageAndHasTheAblityToSendTheErrorBackToTheUiLayer(
                    entry.Key);
            }
        }

        public FlattenExtractedContainer? ExtractModpack(string path)
        {
            using FileStream stream = File.OpenRead(path);
            Result<FlattenExtractedContainer, ExtractError> result =
                _extractor.ExtractAsync(stream, null).GetAwaiter().GetResult();
            if (result.IsSuccessful)
            {
                return result.Value;
            }

            _notification.PopInformation($"Invalid input {Path.GetFileName(path)}: {result.Error}");
            return null;
        }

        public void ApplyExtractedModpack(ModpackPreviewModel model)
        {
            _extractor.SolidifyAsync(model.Inner, model.InstanceName).RunSynchronously();
        }

        public async Task<IEnumerable<MinecraftVersionModel>> FetchVersionAsync()
        {
            PrismIndex manifest =
                await PrismLauncherHelper.GetManifestAsync(PrismLauncherHelper.UID_MINECRAFT, _factory,
                    App.Current.Token);
            return manifest.Versions.Select(x => new MinecraftVersionModel(x.Version, x.Type switch
            {
                PrismReleaseType.Release => ReleaseType.Release,
                PrismReleaseType.Snapshot => ReleaseType.Snapshot,
                PrismReleaseType.Old_Snapshot => ReleaseType.Snapshot,
                PrismReleaseType.Experiment => ReleaseType.Experiment,
                PrismReleaseType.Old_Alpha => ReleaseType.Alpha,
                PrismReleaseType.Old_Beta => ReleaseType.Beta,
                _ => throw new NotImplementedException()
            }, x.ReleaseTime));
        }

        public async Task CreateProfileAsync(string instanceName, string version, MemoryStream? thumbnail)
        {
            ReservedKey key = _profileManager.RequestKey(instanceName);
            if (thumbnail != null)
            {
                thumbnail.Position = 0;
                await _thumbnailSaver.SaveAsync(key.Key, thumbnail);
            }

            _profileManager.Append(key, instanceName, null, new Metadata(version, new List<Metadata.Layer>()));
            _notification.PopInformation($"{instanceName}({version}) has been added");
        }
    }
}