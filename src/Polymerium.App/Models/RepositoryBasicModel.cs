using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models
{
    public partial class RepositoryBasicModel(string label, string name) : ModelBase
    {
        #region Reactive

        [ObservableProperty]
        public partial IReadOnlyList<LoaderBasicModel>? Loaders { get; set; }

        [ObservableProperty]
        public partial IReadOnlyList<string>? Versions { get; set; }

        [ObservableProperty]
        public partial IReadOnlyList<ResourceKind>? Kinds { get; set; }

        #endregion

        #region Direct

        public string Label => label;

        public string Name => name;

        #endregion
    }
}
