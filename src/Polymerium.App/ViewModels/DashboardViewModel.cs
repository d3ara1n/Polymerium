using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using System.Windows.Input;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly InstanceStatusService _instanceStatusService;
        private readonly TridentContext _trident;

        private InstanceStatusModel? status;

        public DashboardViewModel(InstanceStatusService instanceStatusService, TridentContext trident)
        {
            _instanceStatusService = instanceStatusService;
            _trident = trident;

            OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
        }

        public InstanceStatusModel? Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        public ICommand OpenLogFolderCommand { get; }

        public override bool OnAttached(object? maybeKey)
        {
            if (maybeKey is string key)
            {
                Status = _instanceStatusService.MustHave(key);
                return true;
            }

            return false;
        }

        private void OpenLogFolder()
        {
            if (status != null)
            {
                var path = Path.Combine(_trident.InstanceHomePath(status.Key),
                    FileNameHelper.GetAssetFolderName(AssetKind.Log));
                if (Directory.Exists(path))
                {
                    UriFileHelper.OpenInExternal(path);
                }
            }
        }
    }
}