using System;

namespace Polymerium.App.Models;

public class AccountWizardEntryModel
{
    public string Caption { get; set; }
    public string BrandIconSource { get; set; }
    public Type Page { get; set; }
}