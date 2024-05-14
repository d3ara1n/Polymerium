using Polymerium.App.Modals;

namespace Polymerium.App.Services
{
    public class ModalService
    {
        private Action? dismissHandler;
        private Action<ModalBase>? popHandler;

        public void SetPopHandler(Action<ModalBase> handler)
        {
            popHandler = handler;
        }

        public void SetDismissHandler(Action handler)
        {
            dismissHandler = handler;
        }

        public void Pop<T>(T modal)
            where T : ModalBase
        {
            popHandler?.Invoke(modal);
        }

        public void Dimiss()
        {
            dismissHandler?.Invoke();
        }
    }
}