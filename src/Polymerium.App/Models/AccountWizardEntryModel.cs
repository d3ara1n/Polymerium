using System;

namespace Polymerium.App.Models;

public class AccountWizardEntryModel
{
    public AccountWizardEntryModel(string caption, string brandIconSource, Type page)
    {
        Caption = caption;
        BrandIconSource = brandIconSource;
        Page = page;
    }

    public string Caption { get; set; }
    public string BrandIconSource { get; set; }
    public Type Page { get; set; }
}
