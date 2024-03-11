using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Modals;
using Polymerium.App.Services;
using System.Windows.Input;

namespace Polymerium.App.ViewModels
{
    public class AccountViewModel : ObservableObject
    {
        private readonly ModalService _modalService;

        public AccountViewModel(ModalService modalService)
        {
            _modalService = modalService;
            OpenMicrosoftWizardCommand = new RelayCommand(OpenMicrosoftWizard, CanOpenMicrosoftWizard);
            OpenAuthlibWizardCommand = new RelayCommand(OpenAuthlibWizard, CanOpenAuthlibWizard);
        }

        public ICommand OpenMicrosoftWizardCommand { get; }
        public ICommand OpenAuthlibWizardCommand { get; }

        public bool CanOpenMicrosoftWizard()
        {
            return true;
        }

        public void OpenMicrosoftWizard()
        {
            MicrosoftAccountWizardModal modal = new MicrosoftAccountWizardModal();
            _modalService.Pop(modal);
        }

        public bool CanOpenAuthlibWizard()
        {
            return false;
        }

        public void OpenAuthlibWizard()
        {
        }
    }
}