using Newtonsoft.Json;
using Polymerium.Core.Models.Mojang.Converters;
using System.Collections.Generic;

namespace Polymerium.Core.Models.Mojang.Indexes;

public struct LibraryDownloads
{
    public LibraryDownloadsArtifact? Artifact { get; set; }

    [JsonConverter(typeof(ClassifierConverter))]
    public IEnumerable<LibraryDownloadsClassifier> Classifiers { get; set; }
}
