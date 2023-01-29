using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.App.Services;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Polymerium.App.ViewModels
{
    public partial class SettingViewModel : ObservableObject
    {
        private readonly ConfigurationManager _configurationManager;
        public FileBasedLaunchConfiguration Global { get; private set; }

        private string javaPath;
        public string JavaPath { get => javaPath; set { SetProperty(ref javaPath, value); Global.JavaPath = value; } }
        public SettingViewModel(ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            configurationManager.Current.GameGlobals ??= new FileBasedLaunchConfiguration();
            Global = configurationManager.Current.GameGlobals;
            javaPath = Global.JavaPath;
        }
    }
}