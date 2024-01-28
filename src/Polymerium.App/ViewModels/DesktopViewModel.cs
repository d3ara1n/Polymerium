using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Messages;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Extracting;

namespace Polymerium.App.ViewModels;

public class DesktopViewModel : ObservableRecipient, IRecipient<ProfileAddedMessage>
{
    private readonly DispatcherQueue _dispatcher;
    private readonly ModpackExtractor _extractor;
    private readonly NavigationService _navigation;
    private readonly NotificationService _notification;

    public DesktopViewModel(NavigationService navigation, ProfileManager profileManager, ModpackExtractor extractor,
        NotificationService notification)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _navigation = navigation;
        _extractor = extractor;
        _notification = notification;
        GotoInstanceViewCommand = new RelayCommand<string>(GotoInstanceView);

        Entries = new ObservableCollection<EntryModel>(profileManager.Managed.Select(x =>
                new EntryModel(x.Key, x.Value.Value, InstanceState.Idle, GotoInstanceViewCommand))
            .OrderByDescending(x => x.LastPlayAtRaw));

        IsActive = true;
    }

    public ObservableCollection<EntryModel> Entries { get; }

    public RelayCommand<string> GotoInstanceViewCommand { get; }

    public void Receive(ProfileAddedMessage message)
    {
        _dispatcher.TryEnqueue(() =>
        {
            Entries.Add(new EntryModel(message.Key, message.Item, InstanceState.Idle, GotoInstanceViewCommand));
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
}