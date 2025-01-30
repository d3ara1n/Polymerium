using Trident.Abstractions.FileModels;

namespace Trident.Abstractions.Importers;

public record ImportedProfileContainer(
    Profile Profile,
    IReadOnlyList<(string, string)> ImportFileNames,
    Uri? IconUrl);