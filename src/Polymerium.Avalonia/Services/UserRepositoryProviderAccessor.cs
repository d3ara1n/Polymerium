using System.Collections.Generic;
using TridentCore.Core.Services;

namespace Polymerium.Avalonia.Services;

public class UserRepositoryProviderAccessor : IRepositoryProviderAccessor
{
    #region IRepositoryProviderAccessor Members

    public IReadOnlyList<IRepositoryProviderAccessor.ProviderProfile> Build() => [];
    public IReadOnlyList<IRepositoryProviderAccessor.ProviderCustom> BuildCustom() => [];

    #endregion
}
