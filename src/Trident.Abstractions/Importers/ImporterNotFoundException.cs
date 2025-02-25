namespace Trident.Abstractions.Importers;

public class ImporterNotFoundException(string? message = null)
    : Exception(message ?? "The downloaded file has no matched importer type");