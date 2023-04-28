using Polymerium.Abstractions.Resources;
using Polymerium.Core.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.App.Models
{
    public class InstanceUpdateModel
    {
        public InstanceUpdateModel(
            string caption,
            Uri? iconSource,
            string author,
            ResourceType type,
            RepositoryAssetMeta resource
        )
        {
            Caption = caption;
            IconSource = iconSource;
            Author = author;
            Type = type;
            Resource = resource;
        }

        public string Caption { get; set; }
        public Uri? IconSource { get; set; }
        public string Author { get; set; }
        public ResourceType Type { get; set; }
        public RepositoryAssetMeta Resource { get; set; }
    }
}
