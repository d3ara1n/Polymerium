using System.Collections.Generic;
using Polymerium.Trident.Services;

namespace Polymerium.App.Services;

public class UserRepositoryProviderAccessor : IRepositoryProviderAccessor
{
    #region IRepositoryProviderAccessor Members

    public IReadOnlyList<IRepositoryProviderAccessor.ProviderProfile> Build() => [];

    #endregion
}
