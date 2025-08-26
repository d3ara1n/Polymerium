using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Trident.Core.Services;
using Trident.Core.Services.Profiles;
using Trident.Core.Utilities;
using Trident.Abstractions;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.ViewModels
{
    public partial class InstancePropertiesViewModel : InstanceViewModelBase
    {
        private ProfileGuard? _owned;

        public InstancePropertiesViewModel(
            ViewBag bag,
            ProfileManager profileManager,
            InstanceManager instanceManager,
            OverlayService overlayService,
            NotificationService notificationService,
            NavigationService navigationService,
            ConfigurationService configurationService,
            PersistenceService persistenceService,
            InstanceService instanceService) : base(bag, instanceManager, profileManager)
        {
            _overlayService = overlayService;
            _notificationService = notificationService;
            _navigationService = navigationService;
            _configurationService = configurationService;
            _persistenceService = persistenceService;
            _instanceService = instanceService;

            SafeCode = Random.Shared.Next(1000, 9999).ToString();
        }

        #region Other

        private string AccessOverrideString<T>(string key)
        {
            if (_owned != null && _owned.Value.Overrides.TryGetValue(key, out var result) && result is T rv)
            {
                return rv.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private bool AccessOverrideBoolean(string key)
        {
            if (_owned != null && _owned.Value.Overrides.TryGetValue(key, out var result) && result is bool rv)
            {
                return rv;
            }

            return false;
        }

        private void WriteOverride(string key, object? value)
        {
            if (_owned == null)
            {
                return;
            }

            if (value is not null and not "")
            {
                _owned.Value.Overrides[key] = value;
            }
            else
            {
                _owned.Value.Overrides.Remove(key);
            }
        }


        private async Task WriteIconAsync()
        {
            // NOTE: 如果监听 ThumbnailOverwrite 改变去写会导致死循环
            try
            {
                var path = ProfileHelper.PickIcon(Basic.Key);
                if (path != null && File.Exists(path))
                {
                    File.Delete(path);
                }

                using var stream = new MemoryStream();
                ThumbnailOverwrite.Save(stream);
                stream.Position = 0;
                var extension = FileHelper.GuessBitmapExtension(stream);
                stream.Position = 0;
                var iconPath = PathDef.Default.FileOfIcon(Basic.Key, extension);
                await using var writer = new FileStream(iconPath, FileMode.Create, FileAccess.Write);
                await stream.CopyToAsync(writer);
            }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex,
                                                Resources
                                                   .InstancePropertiesView_ThumbnailSavingDangerNotificationTitle);
            }
        }

        #endregion

        #region Overrides

        protected override void OnModelUpdated(string key, Profile profile)
        {
            base.OnModelUpdated(key, profile);

            NameOverwrite = profile.Name;
            ThumbnailOverwrite = Basic.Thumbnail;
        }

        protected override Task OnInitializeAsync(CancellationToken token)
        {
            if (ProfileManager.TryGetMutable(Basic.Key, out var guard))
            {
                _owned = guard;
            }

            #region Update Overrides & Perferences

            JavaHomeOverride = AccessOverrideString<string>(Profile.OVERRIDE_JAVA_HOME);
            JavaHomeWatermark = "Auto decide if unset";
            JavaMaxMemoryOverride = AccessOverrideString<uint>(Profile.OVERRIDE_JAVA_MAX_MEMORY);
            JavaMaxMemoryWatermark = _configurationService.Value.GameJavaMaxMemory.ToString();
            JavaAdditionalArgumentsOverride = AccessOverrideString<string>(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS);
            JavaAdditionalArgumentsWatermark = _configurationService.Value.GameJavaAdditionalArguments;
            WindowInitialHeightOverride = AccessOverrideString<uint>(Profile.OVERRIDE_WINDOW_HEIGHT);
            WindowInitialHeightWatermark = _configurationService.Value.GameWindowInitialHeight.ToString();
            WindowInitialWidthOverride = AccessOverrideString<uint>(Profile.OVERRIDE_WINDOW_WIDTH);
            WindowInitialWidthWatermark = _configurationService.Value.GameWindowInitialWidth.ToString();
            BehaviorDeployFastMode = AccessOverrideBoolean(Profile.OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE);
            BehaviorResolveDependency = AccessOverrideBoolean(Profile.OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY);

            #endregion

            return base.OnInitializeAsync(token);
        }

        protected override async Task OnDeinitializeAsync(CancellationToken token)
        {
            if (_owned != null)
            {
                await _owned.DisposeAsync();
            }

            await base.OnDeinitializeAsync(token);
        }

        #endregion

        #region Injected

        private readonly OverlayService _overlayService;
        private readonly NotificationService _notificationService;
        private readonly NavigationService _navigationService;
        private readonly ConfigurationService _configurationService;
        private readonly PersistenceService _persistenceService;
        private readonly InstanceService _instanceService;

        #endregion

        #region Commands

        [RelayCommand]
        private async Task PickFile(TextBox? box)
        {
            if (box != null)
            {
                var path = await _overlayService.RequestFileAsync(Resources.InstancePropertiesView_RequestJavaPrompt,
                                                                  Resources.InstancePropertiesView_RequestJavaTitle);
                if (path != null && File.Exists(path))
                {
                    var dir = Path.GetDirectoryName(Path.GetDirectoryName(path));
                    if (dir != null)
                    {
                        box.Text = dir;
                    }
                }
            }
        }

        [RelayCommand]
        private void CheckIntegrity() => _instanceService.Deploy(Basic.Key, false, BehaviorResolveDependency, true);

        [RelayCommand]
        private void ResetInstance()
        {
            if (!InstanceManager.IsInUse(Basic.Key))
            {
                var dir = PathDef.Default.DirectoryOfBuild(Basic.Key);
                var file = PathDef.Default.FileOfLockData(Basic.Key);
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }

                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }

                    _persistenceService.AppendAction(new(Basic.Key, PersistenceService.ActionKind.Reset, null, null));
                    _notificationService.PopMessage("Instance reset", Basic.Key, NotificationLevel.Success);
                }
                catch (Exception ex)
                {
                    _notificationService.PopMessage(ex);
                }
            }
        }

        [RelayCommand]
        private void DeleteInstance()
        {
            var path = PathDef.Default.FileOfBomb(Basic.Key);
            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, Basic.Key);
            ProfileManager.Remove(Basic.Key);

            if (_navigationService.CanGoBack)
            {
                _navigationService.GoBack();
            }
            else
            {
                _navigationService.Navigate<LandingView>();
            }
        }

        [RelayCommand]
        private void UnlockInstance()
        {
            if (_owned != null)
            {
                _owned.Value.Setup.Source = null;
            }

            var oldSource = Basic.Source;
            Basic.Source = null;
            _persistenceService.AppendAction(new(Basic.Key, PersistenceService.ActionKind.Unlock, oldSource, null));
            _notificationService.PopMessage(Resources.InstancePropertiesView_UnlockingSuccessNotificationPrompt,
                                            Basic.Key,
                                            NotificationLevel.Success);
        }

        [RelayCommand]
        private async Task RemoveThumbnailAsync()
        {
            ThumbnailOverwrite = AssetUriIndex.DIRT_IMAGE_BITMAP;
            await WriteIconAsync();
        }

        [RelayCommand]
        private async Task SelectThumbnailAsync()
        {
            var path = await _overlayService.RequestFileAsync(Resources.InstancePropertiesView_RequestThumbnailPrompt,
                                                              Resources.InstancePropertiesView_RequestThumbnailTitle);
            if (path != null)
            {
                if (FileHelper.IsBitmapFile(path))
                {
                    ThumbnailOverwrite = new(path);
                    await WriteIconAsync();
                }
                else
                {
                    _notificationService.PopMessage(Resources
                                                       .InstancePropertiesView_ThumbnailSettingDangerNotificationPrompt,
                                                    Resources
                                                       .InstancePropertiesView_ThumbnailSettingDangerNotificationTitle,
                                                    NotificationLevel.Warning);
                }
            }
        }

        [RelayCommand]
        private async Task RenameInstance()
        {
            var name = await _overlayService.RequestInputAsync(Resources.InstancePropertiesView_RequestNamePrompt,
                                                               Resources.InstancePropertiesView_RequestNameTitle,
                                                               Basic.Name);
            if (name != null && _owned != null && !string.Equals(name, Basic.Name))
            {
                var oldName = NameOverwrite;
                NameOverwrite = name;
                _owned.Value.Name = name;
                _persistenceService.AppendAction(new(Basic.Key, PersistenceService.ActionKind.Rename, oldName, name));
            }
        }

        [RelayCommand]
        private void GotoSettings() => _navigationService.Navigate<SettingsView>();

        #endregion

        #region Reactive

        [ObservableProperty]
        public required partial Bitmap ThumbnailOverwrite { get; set; }

        [ObservableProperty]
        public required partial string NameOverwrite { get; set; }

        [ObservableProperty]
        public partial string SafeCode { get; set; }

        #endregion

        #region Overrides

        [ObservableProperty]
        public partial string JavaHomeOverride { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string JavaHomeWatermark { get; set; } = string.Empty;

        partial void OnJavaHomeOverrideChanged(string value) => WriteOverride(Profile.OVERRIDE_JAVA_HOME, value);

        [ObservableProperty]
        public partial string JavaMaxMemoryOverride { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string JavaMaxMemoryWatermark { get; set; } = string.Empty;

        partial void OnJavaMaxMemoryOverrideChanged(string value) =>
            WriteOverride(Profile.OVERRIDE_JAVA_MAX_MEMORY,
                          !string.IsNullOrEmpty(value) && uint.TryParse(value, out var ui) ? ui : null);

        [ObservableProperty]
        public partial string JavaAdditionalArgumentsOverride { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string JavaAdditionalArgumentsWatermark { get; set; } = string.Empty;

        partial void OnJavaAdditionalArgumentsOverrideChanged(string value) =>
            WriteOverride(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS, !string.IsNullOrEmpty(value) ? value : null);

        [ObservableProperty]
        public partial string WindowInitialHeightOverride { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string WindowInitialHeightWatermark { get; set; } = string.Empty;

        partial void OnWindowInitialHeightOverrideChanged(string value) =>
            WriteOverride(Profile.OVERRIDE_WINDOW_HEIGHT,
                          !string.IsNullOrEmpty(value) && uint.TryParse(value, out var ui) ? ui : null);

        [ObservableProperty]
        public partial string WindowInitialWidthOverride { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string WindowInitialWidthWatermark { get; set; } = string.Empty;

        partial void OnWindowInitialWidthOverrideChanged(string value) =>
            WriteOverride(Profile.OVERRIDE_WINDOW_WIDTH,
                          !string.IsNullOrEmpty(value) && uint.TryParse(value, out var ui) ? ui : null);

        [ObservableProperty]
        public partial bool BehaviorResolveDependency { get; set; }

        partial void OnBehaviorResolveDependencyChanged(bool value) =>
            WriteOverride(Profile.OVERRIDE_BEHAVIOR_RESOLVE_DEPENDENCY, value);

        [ObservableProperty]
        public partial bool BehaviorDeployFastMode { get; set; }

        partial void OnBehaviorDeployFastModeChanged(bool value) =>
            WriteOverride(Profile.OVERRIDE_BEHAVIOR_DEPLOY_FASTMODE, value);

        #endregion
    }
}
