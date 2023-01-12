using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels
{
    public class InstanceViewModel : ObservableObject
    {
        private readonly AssetStorageService _storageService;
        public InstanceViewModel(AssetStorageService storageService)
        {
            _storageService = storageService;
        }

        private GameInstance instance;
        public GameInstance Instance { get => instance; set => SetProperty(ref instance, value); }

        public void GotInstance(GameInstance instance)
        {
        }
    }
}
