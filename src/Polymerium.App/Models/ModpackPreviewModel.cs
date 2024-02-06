using System.Linq;
using Polymerium.Trident.Services.Extracting;

namespace Polymerium.App.Models;

public record ModpackPreviewModel(FlattenExtractedContainer Inner)
{
    public string InstanceName { get; set; } = Inner.Original.Name;
    public string Version { get; } = Inner.Original.Version;
    public int AttachmentCount => Inner.Layers.SelectMany(x => x.Original.Attachments).Count();

    public string Loader =>
        string.Join(", ",
            Inner.Layers.SelectMany(x => x.Original.Loaders).Select(x =>
                global::Trident.Abstractions.Resources.Loader.MODLOADER_NAME_MAPPINGS.ContainsKey(x.Id)
                    ? global::Trident.Abstractions.Resources.Loader.MODLOADER_NAME_MAPPINGS[x.Id]
                    : x.Id));
}