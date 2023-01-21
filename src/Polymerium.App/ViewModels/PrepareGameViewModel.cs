using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions;

namespace Polymerium.App.ViewModels
{
    public class PrepareGameViewModel : ObservableObject
    {
        private GameInstance instance;
        public GameInstance Instance { get => instance; set => SetProperty(ref instance, value); }
        private string progress;
        public string Progress { get => progress; set => SetProperty(ref progress, value); }

        public void GotInstance(GameInstance instance)
        {
            Instance = instance;
        }
    }
}
