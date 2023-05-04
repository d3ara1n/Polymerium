using Polymerium.Abstractions.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public class SearchCenterResourceTagModel
    {
        public SearchCenterResourceTagModel(ResourceType tag, string display)
        {
            Tag = tag;
            Display = display;
        }

        public ResourceType Tag { get; set; }
        public string Display { get; set; }
    }
}
