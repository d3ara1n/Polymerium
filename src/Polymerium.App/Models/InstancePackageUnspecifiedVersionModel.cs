namespace Polymerium.App.Models
{
    public class InstancePackageUnspecifiedVersionModel : InstancePackageVersionModelBase
    {
        public static readonly InstancePackageUnspecifiedVersionModel Instance = new();

        private InstancePackageUnspecifiedVersionModel() { }
    }
}
