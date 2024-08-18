using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Polymerium.App.Models;
using Polymerium.App.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Polymerium.App.ViewModels;

public class SettingViewModel : ObservableObject
{
    private readonly DispatcherQueue _dispatcher;
    private readonly ILogger _logger;
    private readonly NotificationService _notificationService;

    public SettingViewModel(ILogger<ConfigurationViewModel> logger, NotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        ChooseJava8Command = new AsyncRelayCommand(async () =>
            await ChooseJavaAsync(x => Settings.Java8 = x, nameof(Java8Status)));
        ChooseJava11Command = new AsyncRelayCommand(async () =>
            await ChooseJavaAsync(x => Settings.Java11 = x, nameof(Java11Status)));
        ChooseJava17Command = new AsyncRelayCommand(async () =>
            await ChooseJavaAsync(x => Settings.Java17 = x, nameof(Java17Status)));
        ChooseJava21Command = new AsyncRelayCommand(async () =>
            await ChooseJavaAsync(x => Settings.Java21 = x, nameof(Java21Status)));
        ScanRuntimeCommand = new AsyncRelayCommand(ScanRuntimeAsync);
    }

    public LanguageModel[] Languages { get; } =
    [
        new LanguageModel("en_US", "Chinglish"),
        new LanguageModel("zh_CN", "简体中文")
    ];

    public StyleModel[] Styles { get; } =
    [
        new StyleModel(0, "None"),
        new StyleModel(1, "Acrylic"),
        new StyleModel(2, "Mica"),
        new StyleModel(3, "MicaAlt")
    ];

    public bool IsSuperpowerActivated
    {
        get => Settings.IsSuperpowerActivated;
        set => SetProperty(Settings.IsSuperpowerActivated, value, x => Settings.IsSuperpowerActivated = x);
    }

    public LanguageModel Language
    {
        get => Languages.FirstOrDefault(x => x.Id == Settings.Language) ?? Languages.First();
        set
        {
            if (value.Id != Settings.Language)
            {
                Settings.Language = value.Id;
                OnPropertyChanged();
            }
        }
    }

    public StyleModel Style
    {
        get => Styles.FirstOrDefault(x => x.Id == Settings.Style) ?? Styles.First();
        set
        {
            if (value.Id != Settings.Style)
            {
                Settings.Style = value.Id;
                OnPropertyChanged();
                App.Current.Window.SystemBackdrop = value.Id switch
                {
                    0 => null, 1 => new DesktopAcrylicBackdrop(), 2 => new MicaBackdrop(),
                    3 => new MicaBackdrop { Kind = MicaKind.BaseAlt }, _ => throw new NotImplementedException()
                };
            }
        }
    }


    public string Java8Status => ValidateJava(Settings.Java8);

    public string Java11Status => ValidateJava(Settings.Java11);

    public string Java17Status => ValidateJava(Settings.Java17);

    public string Java21Status => ValidateJava(Settings.Java21);

    public uint GameJvmMaxMemory
    {
        get => Settings.GameJvmMaxMemory;
        set => SetProperty(Settings.GameJvmMaxMemory, value, x => Settings.GameJvmMaxMemory = x);
    }

    public string GameJvmAdditionalArguments
    {
        get => Settings.GameJvmAdditionalArguments;
        set => SetProperty(Settings.GameJvmAdditionalArguments, value,
            x => Settings.GameJvmAdditionalArguments = x);
    }

    public uint GameWindowHeight
    {
        get => Settings.GameWindowHeight;
        set => SetProperty(Settings.GameWindowHeight, value, x => Settings.GameWindowHeight = x);
    }

    public uint GameWindowWidth
    {
        get => Settings.GameWindowWidth;
        set => SetProperty(Settings.GameWindowWidth, value, x => Settings.GameWindowWidth = x);
    }

    public ICommand ChooseJava8Command { get; }
    public ICommand ChooseJava11Command { get; }
    public ICommand ChooseJava17Command { get; }
    public ICommand ChooseJava21Command { get; }
    public ICommand ScanRuntimeCommand { get; }

    private async Task ChooseJavaAsync(Action<string> setter, string propertyName)
    {
        FileOpenPicker picker = new();
        InitializeWithWindow.Initialize(picker,
            WindowNative.GetWindowHandle(App.Current.Window));
        picker.FileTypeFilter.Add(".exe");
        picker.FileTypeFilter.Add("*");
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        var file = await picker.PickSingleFileAsync();
        if (file?.Path != null)
        {
            SetJavaInternal(file.Path, setter, propertyName);
        }
    }

    private void SetJavaInternal(string file, Action<string> setter, string propertyName)
    {
        var home = Path.GetDirectoryName(Path.GetDirectoryName(file));
        setter(home ?? string.Empty);
        OnPropertyChanged(propertyName);
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

    private async Task ScanRuntimeAsync()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        _notificationService.PopInformation("Found nothing.\n(Maybe it's just a placebo button?)");
    }
}