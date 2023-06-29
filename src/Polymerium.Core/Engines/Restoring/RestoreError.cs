namespace Polymerium.Core.Engines.Restoring;

public enum RestoreError
{
    ResourceNotReacheable,
    ResourceNotFound,
    SerializationFailure,
    ComponentInstallationFailure,
    Canceled,
    IOException,
    ComponentNotFound
}
