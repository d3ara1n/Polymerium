using Polymerium.App.Models;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels
{
    public class DashboardViewModel(InstanceStatusService instanceStatusService) : ViewModelBase
    {
        private InstanceStatusModel? status;

        public InstanceStatusModel? Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        public override bool OnAttached(object? maybeKey)
        {
            if (maybeKey is string key)
            {
                Status = instanceStatusService.MustHave(key);
                return true;
            }

            return false;
        }
    }
}