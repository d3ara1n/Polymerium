using Polymerium.App.Modals;
using System;

namespace Polymerium.App.Services
{
    public class ModalService
    {
        private Action<ModalBase>? popHandler;

        public void SetHandler(Action<ModalBase> handler)
        {
            popHandler = handler;
        }

        public void Pop<T>(T modal)
            where T : ModalBase
        {
            popHandler?.Invoke(modal);
        }
    }
}