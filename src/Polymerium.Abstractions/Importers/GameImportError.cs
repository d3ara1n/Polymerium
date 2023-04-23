namespace Polymerium.Abstractions.Importers;

public enum GameImportError
{
    Unknown,
    Cancelled,
    FileSystemError,
    ResourceNotFound,
    BrokenIndex,
    Unsupported,
    WrongPackType
}
