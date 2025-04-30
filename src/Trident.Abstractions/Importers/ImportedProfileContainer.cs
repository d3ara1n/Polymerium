using Trident.Abstractions.FileModels;

namespace Trident.Abstractions.Importers;

public record ImportedProfileContainer(
    Profile Profile,
    IReadOnlyList<(string Source, string Target)> ImportFileNames,
    Uri? IconUrl);