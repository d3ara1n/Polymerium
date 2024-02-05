using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ViewModelBase
{
    private readonly TridentContext _context;
    private readonly NavigationService _navigation;
    private readonly ProfileManager _profileManager;

    private ProfileModel model = new ProfileModel(ProfileManager.DUMMY_KEY, ProfileManager.DUMMY_PROFILE);

    public InstanceViewModel(ProfileManager profileManager, NavigationService navigation, TridentContext context)
    {
        _profileManager = profileManager;
        _navigation = navigation;
        _context = context;

        GotoWorkbenchViewCommand = new RelayCommand<string>(GotoWorkbenchView);
        OpenHomeFolderCommand = new RelayCommand(OpenHomeFolder, CanOpenHomeFolder);
        OpenAssetFolderCommand = new RelayCommand<AssetKind>(OpenAssetFolder, CanOpenAssetFolder);
        DeleteTodoCommand = new RelayCommand<TodoModel>(DeleteTodo, CanDeleteTodo);
    }

    public ProfileModel Model
    {
        get => model;
        set => SetProperty(ref model, value);
    }

    public ICommand GotoWorkbenchViewCommand { get; }
    public ICommand OpenAssetFolderCommand { get; }
    public ICommand OpenHomeFolderCommand { get; }
    public ICommand DeleteTodoCommand { get; }

    public override bool OnAttached(object? maybeKey)
    {
        if (maybeKey is string key)
        {
            var profile = _profileManager.GetProfile(key);
            if (profile != null)
                Model = new ProfileModel(key, profile);
            return profile != null;
        }

        return false;
    }

    public override void OnDetached()
    {
        if (Model.Key != ProfileManager.DUMMY_KEY) _profileManager.Flush(Model.Key);
    }

    private void GotoWorkbenchView(string? key)
    {
        if (key != null && key != ProfileManager.DUMMY_KEY)
            _navigation.Navigate(typeof(WorkbenchView), key, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
    }

    private string GetHomeFolderPath()
    {
        return Path.Combine(_context.InstanceDir, Model.Key);
    }

    private string GetAssetFolderPath(AssetKind kind)
    {
        return Path.Combine(GetHomeFolderPath(), kind switch
        {
            AssetKind.Mod => "mods",
            AssetKind.Save => "saves",
            AssetKind.Screenshot => "screenshots",
            AssetKind.ShaderPack => "shaders",
            AssetKind.ResourcePack => "resourcepacks",
            AssetKind.DataPack => "datapacks",
            _ => throw new NotImplementedException()
        });
    }

    private bool CanOpenHomeFolder()
    {
        return Directory.Exists(GetHomeFolderPath());
    }

    private void OpenHomeFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = GetHomeFolderPath(),
            UseShellExecute = true
        });
    }

    private bool CanOpenAssetFolder(AssetKind kind)
    {
        return false;
    }

    private void OpenAssetFolder(AssetKind kind)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = GetAssetFolderPath(kind),
            UseShellExecute = true
        });
    }

    public void AddTodo(string text)
    {
        Model.Todos.Add(new TodoModel(new Profile.RecordData.Todo(false, text)));
    }

    private bool CanDeleteTodo(TodoModel? item)
    {
        return item != null;
    }

    private void DeleteTodo(TodoModel? item)
    {
        if (item != null) Model.Todos.Remove(item);
    }
}