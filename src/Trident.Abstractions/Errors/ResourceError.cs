namespace Trident.Abstractions.Errors;

public enum ResourceError
{
    Unknown,
    Cancelled,
    NotFound,
    InvalidParameter,
    BadNetwork,
    InvalidFormat,
    Unsupported,
    BadCommunication
}