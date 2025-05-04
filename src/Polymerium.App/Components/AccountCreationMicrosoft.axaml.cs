using System;
using Polymerium.App.Controls;

namespace Polymerium.App.Components;

public partial class AccountCreationMicrosoft : AccountCreationStep
{
    public AccountCreationMicrosoft()
    {
        InitializeComponent();
    }

    public override object NextStep() => throw new NotImplementedException();
}