using System;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace Polymerium.App.Controls
{
    public abstract class AccountCreationStep : HeaderedContentControl
    {
        public static readonly DirectProperty<AccountCreationStep, bool> IsNextAvailableProperty =
            AvaloniaProperty.RegisterDirect<AccountCreationStep, bool>(nameof(IsNextAvailable),
                                                                       o => o.IsNextAvailable,
                                                                       (o, v) => o.IsNextAvailable = v);

        protected override Type StyleKeyOverride => typeof(AccountCreationStep);

        public bool IsNextAvailable
        {
            get;
            set => SetAndRaise(IsNextAvailableProperty, ref field, value);
        }

        public abstract object NextStep();
    }
}
