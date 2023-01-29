namespace Polymerium.Core.Engines.Restoring
{
    public enum RestoreError
    {
        ResourceNotReacheable,
        ResourceNotFound,
        SerializationFailure,
        OsNotSupport,
        Canceled,
        ExceptionOccurred,
        Unknown
    }
}