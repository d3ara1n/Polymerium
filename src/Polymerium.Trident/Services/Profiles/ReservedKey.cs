namespace Polymerium.Trident.Services.Profiles
{
    public class ReservedKey : IDisposable
    {
        private readonly ProfileManager _root;

        internal ReservedKey(string key, ProfileManager root)
        {
            _root = root;
            Key = key;
        }

        public string Key { get; }

        #region IDisposable Members

        public void Dispose() =>
            // 可能会遇到临界问题，但概率很低
            _root.ReservedKeys.Remove(this);

        #endregion
    }
}
