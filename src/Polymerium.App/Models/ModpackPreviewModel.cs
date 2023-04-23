using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public class ModpackPreviewModel
    {
        public ModpackPreviewModel(string name, string version, string author)
        {
            Name = name;
            Version = version;
            Author = author;
        }

        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
    }
}
