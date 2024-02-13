using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services;
using WinRT.Interop;

namespace Polymerium.App.ViewModels;

public class ConfigurationViewModel : ViewModelBase
{
    private readonly InstanceStatusService _instanceStatusService;
    private readonly ProfileManager _profileManager;
    private readonly ThumbnailSaver _thumbnailSaver;

    private ProfileModel model = ProfileModel.DUMMY;

    public ConfigurationViewModel(ProfileManager profileManager, ThumbnailSaver thumbnailSaver,
        InstanceStatusService instanceStatusService)
    {
        _profileManager = profileManager;
        _thumbnailSaver = thumbnailSaver;
        _instanceStatusService = instanceStatusService;

        ChooseJavaCommand = new RelayCommand(ChooseJava, CanChooseJava);
        OpenExportWizardCommand = new RelayCommand(OpenExportWizard, CanOpenExportWizard);
    }

    public ProfileModel Model
    {
        get => model;
        set => SetProperty(ref model, value);
    }

    public IRelayCommand ChooseJavaCommand { get; }
    public ICommand OpenExportWizardCommand { get; }

    public bool IsWindowSizeOverridden
    {
        get => IsKeyOverridden(Settings.GAME_WINDOW_WIDTH) && IsKeyOverridden(Settings.GAME_WINDOW_HEIGHT);
        set
        {
            if (value)
            {
                SetToDefault<uint>(Settings.GAME_WINDOW_WIDTH, nameof(WindowWidth));
                SetToDefault<uint>(Settings.GAME_WINDOW_HEIGHT, nameof(WindowHeight));
            }
            else
            {
                ClearValue(Settings.GAME_WINDOW_WIDTH, nameof(WindowWidth));
                ClearValue(Settings.GAME_WINDOW_HEIGHT, nameof(WindowHeight));
            }
        }
    }

    public uint WindowHeight
    {
        get => GetValue<uint>(Settings.GAME_WINDOW_HEIGHT);
        set => SetValue(Settings.GAME_WINDOW_HEIGHT, value, nameof(WindowHeight));
    }

    public uint WindowWidth
    {
        get => GetValue<uint>(Settings.GAME_WINDOW_WIDTH);
        set => SetValue(Settings.GAME_WINDOW_WIDTH, value, nameof(WindowWidth));
    }

    public bool IsJvmHomeOverridden
    {
        get => IsKeyOverridden(Settings.GAME_JVM_HOME);
        set
        {
            if (value)
                SetToDefault<string>(Settings.GAME_JVM_HOME, nameof(JvmHomeStatus));
            else
                ClearValue(Settings.GAME_JVM_HOME, nameof(JvmHomeStatus));
            ChooseJavaCommand.NotifyCanExecuteChanged();
        }
    }

    public string JvmHomeStatus => ValidateJava(GetValue<string>(Settings.GAME_JVM_HOME));

    public bool IsJvmMaxMemoryOverridden
    {
        get => IsKeyOverridden(Settings.GAME_JVM_MAX_MEMORY);
        set
        {
            if (value)
                SetToDefault<uint>(Settings.GAME_JVM_MAX_MEMORY, nameof(JvmMaxMemory));
            else
                ClearValue(Settings.GAME_JVM_MAX_MEMORY, nameof(JvmMaxMemory));
        }
    }

    public uint JvmMaxMemory
    {
        get => GetValue<uint>(Settings.GAME_JVM_MAX_MEMORY);
        set => SetValue(Settings.GAME_JVM_MAX_MEMORY, value, nameof(JvmMaxMemory));
    }

    public bool IsJvmAdditionalArgumentsOverridden
    {
        get => IsKeyOverridden(Settings.GAME_JVM_ADDITIONAL_ARGUMENTS);
        set
        {
            if (value)
                SetToDefault<string>(Settings.GAME_JVM_ADDITIONAL_ARGUMENTS, nameof(JvmAdditionalArguments));
            else
                ClearValue(Settings.GAME_JVM_ADDITIONAL_ARGUMENTS, nameof(JvmAdditionalArguments));
        }
    }

    public string JvmAdditionalArguments
    {
        get => GetValue<string>(Settings.GAME_JVM_ADDITIONAL_ARGUMENTS);
        set => SetValue(Settings.GAME_JVM_ADDITIONAL_ARGUMENTS, value, nameof(JvmAdditionalArguments));
    }

    public override bool OnAttached(object? maybeKey)
    {
        if (maybeKey is string key)
        {
            var profile = _profileManager.GetProfile(key);
            if (profile != null)
                Model = new ProfileModel(key, profile, _thumbnailSaver.Get(key), _instanceStatusService.MustHave(key));
            return profile != null;
        }

        return false;
    }

    public override void OnDetached()
    {
        if (Model.Key != ProfileManager.DUMMY_KEY) _profileManager.Flush(Model.Key);
    }

    private string ValidateJava(string home)
    {
        if (Directory.Exists(home))
        {
            var path = Path.Combine(home, "bin", "java.exe");
            if (File.Exists(path))
            {
                var version = FileVersionInfo.GetVersionInfo(path);
                return $"{version.ProductName ?? "Unknown"}({home})";
            }
        }

        return string.IsNullOrEmpty(home) ? "Unset" : $"Unknown({home})";
    }

    private bool CanOpenExportWizard()
    {
        return false;
    }

    private void OpenExportWizard()
    {
    }

    private bool CanChooseJava()
    {
        return IsJvmHomeOverridden;
    }

    private async void ChooseJava()
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker,
            WindowNative.GetWindowHandle(App.Current.Window));
        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add("*");
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        var file = await picker.PickSingleFileAsync();
        if (file.Path != null)
        {
            var home = Path.GetDirectoryName(Path.GetDirectoryName(file.Path));
            if (home != null)
                SetValue(Settings.GAME_JVM_HOME, home, nameof(JvmHomeStatus));
        }
    }

    private bool IsKeyOverridden(string key)
    {
        return Model.Inner.Overrides.ContainsKey(key);
    }

    private void SetToDefault<T>(string key, string propertyName)
    {
        Model.Inner.Overrides[key] = Settings.GetValue<T>(key)!;
    }

    private void ClearValue(string key, string propertyName)
    {
        if (Model.Inner.Overrides.ContainsKey(key))
        {
            Model.Inner.Overrides.Remove(key);
            OnPropertyChanged(propertyName);
        }
    }

    private T GetValue<T>(string key)
    {
        if (Model.Inner.Overrides.TryGetValue(key, out var v) && v is T r) return r;

        return Settings.GetValue<T>(key);
    }

    private void SetValue<T>(string key, T value, string propertyName)
    {
        var old = GetValue<T>(key);
        if (value != null && old != null && !old.Equals(value))
        {
            Model.Inner.Overrides[key] = value;
            OnPropertyChanged(propertyName);
        }
    }
}