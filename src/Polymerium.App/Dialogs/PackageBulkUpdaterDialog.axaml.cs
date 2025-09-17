using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions.Utilities;
using Trident.Core.Services.Profiles;

namespace Polymerium.App.Dialogs;

public partial class PackageBulkUpdaterDialog : Dialog
{
    public PackageBulkUpdaterDialog() => InitializeComponent();

    protected override bool ValidateResult(object? result) => true;
}
