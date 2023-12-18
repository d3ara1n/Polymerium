using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Polymerium.App.Models
{
    public record NavItem(string Key, string IconKey, Type View);
}
