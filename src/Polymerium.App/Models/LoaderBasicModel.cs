namespace Polymerium.App.Models
{
    public class LoaderBasicModel(string loaderId, string displayName)
    {
        public string LoaderId { get; } = loaderId;
        public string DisplayName { get; } = displayName;
    }
}
