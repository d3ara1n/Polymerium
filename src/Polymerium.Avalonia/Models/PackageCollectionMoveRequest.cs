using TridentCore.Abstractions.FileModels;

namespace Polymerium.Avalonia.Models;

public sealed record PackageCollectionMoveRequest(Profile.Rice.Entry Package, string TargetSource);
