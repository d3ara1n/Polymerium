using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Huskui.Avalonia.Models;

// Connect DataTemplates to IDataTemplate
public class RelayDataTemplate : AvaloniaList<IDataTemplate>, IDataTemplate
{

    public Control? Build(object? param)
    {
        var match = this.FirstOrDefault(x => x.Match(param));
        if (match is not null)
            return match.Build(param);

        return param as Control;
    }

    public bool Match(object? data) => this.Any(x => x.Match(data)) || data is Control;
    
}