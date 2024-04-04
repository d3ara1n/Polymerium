using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels
{
    public class InstanceViewModel : ViewModelBase
    {
        private readonly AccountManager _accountManager;
        private readonly TridentContext _context;
        private readonly InstanceManager _instanceManager;
        private readonly InstanceService _instanceService;
        private readonly InstanceStatusService _instanceStatusService;
        private readonly NavigationService _navigation;
        private readonly NotificationService _notification;
        private readonly ProfileManager _profileManager;
        private readonly ThumbnailSaver _thumbnailSaver;

        private readonly Bindable<AccountManager, string?> defaultUuid;
        private AccountModel? account;

        private ProfileModel profile = ProfileModel.DUMMY;

        public InstanceViewModel(ProfileManager profileManager, NavigationService navigation, TridentContext context,
            ThumbnailSaver thumbnailSaver, InstanceManager instanceManager,
            InstanceStatusService instanceStatusService, InstanceService instanceService,
            NotificationService notification, AccountManager accountManager)
        {
            _profileManager = profileManager;
            _navigation = navigation;
            _context = context;
            _thumbnailSaver = thumbnailSaver;
            _instanceManager = instanceManager;
            _instanceStatusService = instanceStatusService;
            _instanceService = instanceService;
            _notification = notification;
            _accountManager = accountManager;

            // NOTE: 在 InstanceViewModel 生命周期内，accountManager.DefaultUuid 不应该被修改。因此此处改成 Dummy 对象。
            defaultUuid = Bindable<AccountManager, string?>.CreateDummy(_accountManager, null);

            GotoMetadataViewCommand = new RelayCommand<string>(GotoMetadataView);
            GotoConfigurationViewCommand = new RelayCommand<string>(GotoConfigurationView);
            OpenHomeFolderCommand = new RelayCommand(OpenHomeFolder, CanOpenHomeFolder);
            OpenAssetFolderCommand = new RelayCommand<AssetKind>(OpenAssetFolder, CanOpenAssetFolder);
            DeleteTodoCommand = new RelayCommand<TodoModel>(DeleteTodo, CanDeleteTodo);
            StopCommand = new RelayCommand(Stop);
            PlayCommand = new RelayCommand(Play);
            GotoDashboardViewCommand = new RelayCommand(GotoDashboardView);
        }

        public ProfileModel Profile
        {
            get => profile;
            set => SetProperty(ref profile, value);
        }

        public AccountModel? Account
        {
            get => account;
            set => SetProperty(ref account, value);
        }

        public ICommand GotoMetadataViewCommand { get; }
        public ICommand GotoConfigurationViewCommand { get; }
        public ICommand OpenAssetFolderCommand { get; }
        public ICommand OpenHomeFolderCommand { get; }
        public ICommand DeleteTodoCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand GotoDashboardViewCommand { get; }

        public override bool OnAttached(object? maybeKey)
        {
            if (maybeKey is string key)
            {
                var got = _profileManager.GetProfile(key);
                if (got != null)
                {
                    Profile = new ProfileModel(key, got, _thumbnailSaver.Get(key),
                        _instanceStatusService.MustHave(key));
                    if (got.AccountId != null && _accountManager.TryGetByUuid(got.AccountId, out var result))
                    {
                        Account = new AccountModel(result, defaultUuid, DummyCommand.Instance, DummyCommand.Instance);
                    }
                }

                return got != null;
            }

            return false;
        }

        public override void OnDetached()
        {
            if (Profile.Key != ProfileManager.DUMMY_KEY)
            {
                _profileManager.Flush(Profile.Key);
            }
        }

        private void GotoMetadataView(string? key)
        {
            if (key != null && key != ProfileManager.DUMMY_KEY)
            {
                _navigation.Navigate(typeof(MetadataView), key,
                    new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
            }
        }

        private void GotoConfigurationView(string? key)
        {
            if (key != null && key != ProfileManager.DUMMY_KEY)
            {
                _navigation.Navigate(typeof(ConfigurationView), key,
                    new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
            }
        }

        private string GetHomeFolderPath()
        {
            return _context.InstanceHomePath(Profile.Key);
        }

        private string GetAssetFolderPath(AssetKind kind)
        {
            return Path.Combine(GetHomeFolderPath(), FileNameHelper.GetAssetFolderName(kind));
        }

        private bool CanOpenHomeFolder()
        {
            return Directory.Exists(GetHomeFolderPath());
        }

        private void OpenHomeFolder()
        {
            UriFileHelper.OpenInExternal(GetHomeFolderPath());
        }

        private bool CanOpenAssetFolder(AssetKind kind)
        {
            var path = GetAssetFolderPath(kind);
            return Directory.Exists(path);
        }

        private void OpenAssetFolder(AssetKind kind)
        {
            UriFileHelper.OpenInExternal(GetAssetFolderPath(kind));
        }

        public void AddTodo(string text)
        {
            Profile.Todos.Add(new TodoModel(new Profile.RecordData.Todo(false, text)));
        }

        private bool CanDeleteTodo(TodoModel? item)
        {
            return item != null;
        }

        private void DeleteTodo(TodoModel? item)
        {
            if (item != null)
            {
                Profile.Todos.Remove(item);
            }
        }

        private void Play()
        {
            _instanceService.DeployAndLaunchSafelyBecauseThisIsUiPackageAndHasTheAblityToSendTheErrorBackToTheUiLayer(
                Profile.Key);
        }

        private void Stop()
        {
            if (_instanceManager.IsTracking(Profile.Key, out var tracker))
            {
                switch (tracker)
                {
                    case DeployTracker deployer:
                        deployer.Abort();
                        break;
                    case LaunchTracker launcher:
                        break;
                }
            }
        }

        private void GotoDashboardView()
        {
            _navigation.Navigate(typeof(DashboardView), Profile.Key,
                new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        public IList<AccountModel> GetAccountCandidates()
        {
            return _accountManager.Managed.Select(x =>
                new AccountModel(x, defaultUuid, DummyCommand.Instance, DummyCommand.Instance)).ToList();
        }

        public void SwitchAccountTo(AccountModel model)
        {
            Profile.Inner.AccountId = model.Inner.Uuid;
            Account = model;
        }
    }
}