namespace Polymerium.Abstractions.Importers;

public enum GameImportError
{
    Unknown,
    FileSystemError,
    ResourceNotFound,
    BrokenIndex,
    Unsupported,
    WrongPackType
}
